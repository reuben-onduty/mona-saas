﻿// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Mona.SaaS.Core.Interfaces;
using Mona.SaaS.Core.Models.Configuration;
using Mona.SaaS.Web.Models;
using Mona.SaaS.Web.Models.Admin;
using System;
using System.Threading.Tasks;

namespace Mona.SaaS.Web.Controllers
{
    [Authorize]
    public class AdminController : Controller
    {
        private readonly DeploymentConfiguration deploymentConfig;
        private readonly IdentityConfiguration identityConfig;
        private readonly ILogger logger;
        private readonly IPublisherConfigurationStore publisherConfigStore;

        public AdminController(
            IOptionsSnapshot<DeploymentConfiguration> deploymentConfig,
            IOptionsSnapshot<IdentityConfiguration> identityConfig,
            ILogger<AdminController> logger,
            IPublisherConfigurationStore publisherConfigStore)
        {
            this.deploymentConfig = deploymentConfig.Value;
            this.identityConfig = identityConfig.Value;
            this.publisherConfigStore = publisherConfigStore;
            this.logger = logger;
        }

        [HttpGet, Route("admin", Name = "admin")]
        public async Task<IActionResult> Index()
        {
            try
            {
                var publisherConfig = await this.publisherConfigStore.GetPublisherConfiguration();

                if (publisherConfig == null) // No publisher config. Publisher needs to complete setup.
                {
                    return RedirectToRoute("setup");
                }

                var adminModel = new AdminPageModel
                {
                    IsTestModeEnabled = this.deploymentConfig.IsTestModeEnabled,
                    IsPassthroughModeEnabled = this.deploymentConfig.IsPassthroughModeEnabled,
                    MonaVersion = this.deploymentConfig.MonaVersion,
                    AzureSubscriptionId = this.deploymentConfig.AzureSubscriptionId,
                    AzureResourceGroupName = this.deploymentConfig.AzureResourceGroupName,
                    EventGridTopicOverviewUrl = GetEventGridTopicUrl(),
                    ConfigurationSettingsUrl = GetConfigurationSettingsEditorUrl(),
                    PartnerCenterTechnicalDetails = GetPartnerCenterTechnicalDetails(),
                    ResourceGroupOverviewUrl = GetResourceGroupUrl(),
                    TestLandingPageUrl = Url.RouteUrl("landing/test", null, Request.Scheme),
                    TestWebhookUrl = Url.RouteUrl("webhook/test", null, Request.Scheme),
                    UserManagementUrl = GetUserManagementUrl()
                };

                return View(adminModel);
            }
            catch (Exception ex)
            {
                this.logger.LogError(ex, "An error occurred while attempting to load the Mona admin page. See exception for details.");

                throw;
            }
        }

        private string GetUserManagementUrl() =>
            $"https://portal.azure.com/#blade/Microsoft_AAD_IAM/ManagedAppMenuBlade/Users" +
            $"/objectId/{this.identityConfig.AppIdentity.AadPrincipalId}" +
            $"/appId/{this.identityConfig.AppIdentity.AadClientId}";

        private string GetConfigurationSettingsEditorUrl() =>
            $"https://portal.azure.com/#@{this.identityConfig.AppIdentity.AadTenantId}/resource/subscriptions/{this.deploymentConfig.AzureSubscriptionId}" +
            $"/resourceGroups/{this.deploymentConfig.AzureResourceGroupName}/providers/Microsoft.Web" +
            $"/sites/mona-web-{this.deploymentConfig.Name.ToLower()}/configuration";

        private string GetEventGridTopicUrl() =>
            $"https://portal.azure.com/#@{this.identityConfig.AppIdentity.AadTenantId}/resource/subscriptions/{this.deploymentConfig.AzureSubscriptionId}" +
            $"/resourceGroups/{this.deploymentConfig.AzureResourceGroupName}/providers/Microsoft.EventGrid" +
            $"/topics/mona-events-{this.deploymentConfig.Name.ToLower()}/overview";

        private string GetResourceGroupUrl() =>
            $"https://portal.azure.com/#@{this.identityConfig.AppIdentity.AadTenantId}/resource/subscriptions/{this.deploymentConfig.AzureSubscriptionId}" +
            $"/resourceGroups/{this.deploymentConfig.AzureResourceGroupName}/overview";

        private PartnerCenterTechnicalDetails GetPartnerCenterTechnicalDetails() =>
            new PartnerCenterTechnicalDetails
            {
                AadApplicationId = this.identityConfig.ManagedIdentities.ExternalClientId,
                AadTenantId = this.identityConfig.AdminIdentity.AadTenantId,
                LandingPageUrl = Url.RouteUrl("landing", null, Request.Scheme),
                WebhookUrl = Url.RouteUrl("webhook", null, Request.Scheme)
            };
    }
}