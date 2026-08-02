using Microsoft.AspNetCore.HttpOverrides;
using ServiceDefaults;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();
builder.AddDefaultProblemDetails();

builder.Services
    .AddReverseProxy()
    .LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"))
    .AddServiceDiscoveryDestinationResolver();

var app = builder.Build();

app.UseForwardedHeaders(new ForwardedHeadersOptions
{
    ForwardedHeaders = ForwardedHeaders.XForwardedFor |
                       ForwardedHeaders.XForwardedHost |
                       ForwardedHeaders.XForwardedProto
});

app.UseDefaultProblemDetails();

app.MapReverseProxy();
app.MapDefaultEndpoints();

app.Run();

public partial class Program { }
