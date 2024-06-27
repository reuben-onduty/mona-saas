using Mona.SaaS.Core.Interfaces;
using Mona.SaaS.Core.Models.Configuration;
using Mona.SaaS.Services;

// Let's build ourselves an admin app...
var builder = WebApplication.CreateBuilder(args);

// Get the configuration...
var configuration = builder.Configuration;

// Wire up Mona services...
builder.Services.AddTransient<IMarketplaceOperationService, DefaultMarketplaceClient>()
                .AddTransient<IMarketplaceSubscriptionService, DefaultMarketplaceClient>()
                .AddTransient<ISubscriptionStagingCache, BlobStorageSubscriptionStagingCache>()
                .AddTransient<ISubscriptionEventPublisher, EventGridSubscriptionEventPublisher>()
                .AddTransient<IPublisherConfigurationStore, BlobStoragePublisherConfigurationStore>();

// Wire up configuration...
builder.Services.Configure<DeploymentConfiguration>(configuration.GetSection("Deployment"))
                .Configure<IdentityConfiguration>(configuration.GetSection("Identity"))
                .Configure<EventGridSubscriptionEventPublisher.Configuration>(configuration.GetSection("Subscriptions:Events:EventGrid"))
                .Configure<BlobStorageSubscriptionStagingCache.Configuration>(configuration.GetSection("Subscriptions:Staging:Cache:BlobStorage"))
                .Configure<BlobStoragePublisherConfigurationStore.Configuration>(configuration.GetSection("PublisherConfig:Store:BlobStorage"));

// Add services to the container.
builder.Services.AddControllersWithViews();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();