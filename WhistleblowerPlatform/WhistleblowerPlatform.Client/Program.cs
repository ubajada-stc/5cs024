using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using WhistleblowerPlatform.Client;
var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

//if (builder.HostEnvironment.IsDevelopment())
//{
//    builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri("https://localhost:7178/") });
//}
//else
//{
//    builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });
//}

if (builder.HostEnvironment.BaseAddress.Contains("localhost"))
{
    builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri("https://localhost:7178/") });
}
else
{
    builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });
}

builder.Services.AddScoped<WhistleblowerPlatform.Client.Services.CryptoService>();
builder.Services.AddScoped<WhistleblowerPlatform.Client.Services.InvestigatorKeyService>();
builder.Services.AddScoped<WhistleblowerPlatform.Client.Services.AuthService>();
builder.Services.AddScoped<WhistleblowerPlatform.Client.Services.InvestigatorReportService>();
await builder.Build().RunAsync();
