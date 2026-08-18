using Gateway.API;
using Scalar.AspNetCore;
using ServiceDefaults;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults("gateway-api");
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

var apiDocumentationEnabled =
    builder.Configuration.GetValue<bool>("ApiDocumentation:Enabled");

var isApiDocumentationEnvironment =
    app.Environment.IsDevelopment() ||
    app.Environment.IsStaging();

// Mở document cho env staging 
if (apiDocumentationEnabled && isApiDocumentationEnvironment)
{
    app.MapScalarApiReference("/scalar", options =>
    {
        options.DefaultFonts = false;

        options.WithOpenApiRoutePattern("/openapi/{documentName}/v1.json");

        options
            .AddDocument("catalog", "Catalog API", "/openapi/catalog/v1.json", isDefault: true)
            .AddDocument("seat", "Seat API","/openapi/seat/v1.json")
            .AddDocument("booking","Booking API","/openapi/booking/v1.json");
    });
}

// app.MapReverseProxy();
// Route timeout metadata requires UseRequestTimeouts() to run after routing
// and before the reverse-proxy endpoints execute.
app.MapReverseProxy();
app.MapDefaultEndpoints();

app.Run();

public partial class Program { }
