using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using FrosthavenCompanion.App;
using FrosthavenCompanion.App.Services;
using FrosthavenCompanion.Domain;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });
builder.Services.AddSingleton(_ => ScenarioCatalog.LoadEmbedded());
builder.Services.AddSingleton(_ => MonsterCatalog.LoadEmbedded());
builder.Services.AddSingleton<CampaignEngine>();
builder.Services.AddScoped<GistSyncService>();
builder.Services.AddScoped<CampaignStore>();
builder.Services.AddScoped<MonsterCardService>();

await builder.Build().RunAsync();
