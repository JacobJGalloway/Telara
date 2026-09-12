using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Telara.Client;
using Telara.Client.Services;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

var opsApiBaseUrl = builder.Configuration["OpsApi:BaseUrl"]
    ?? throw new InvalidOperationException("OpsApi:BaseUrl is not configured (wwwroot/appsettings.json).");

builder.Services.AddHttpClient<GraphQlClient>(client => client.BaseAddress = new Uri(opsApiBaseUrl));

await builder.Build().RunAsync();
