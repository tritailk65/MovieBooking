using Gateway.API;
using ServiceDefaults;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();
builder.AddDefaultProblemDetails();
builder.AddGatewayTransportSecurity();
builder.Services.AddRequestTimeouts();

builder.Services
    .AddReverseProxy()
    .LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"))
    .AddServiceDiscoveryDestinationResolver();

var app = builder.Build();

// app.UseForwardedHeaders(new ForwardedHeadersOptions { ... });
// The new configuration validates and trusts only explicitly configured proxies.
app.UseForwardedHeaders();

app.UseDefaultProblemDetails();

if (builder.Configuration.GetValue<bool>("Https:UseHsts"))
{
    app.UseHsts();
}

if (builder.Configuration.GetValue<bool>("Https:RedirectAtGateway"))
{
    app.UseHttpsRedirection();
}

app.UseRouting();
app.UseRequestTimeouts();

// app.MapReverseProxy();
// Route timeout metadata requires UseRequestTimeouts() to run after routing
// and before the reverse-proxy endpoints execute.
app.MapReverseProxy();
app.MapDefaultEndpoints();

app.Run();

public partial class Program { }
