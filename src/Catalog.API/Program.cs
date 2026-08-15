using Catalog.API;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();
builder.AddApplicationServices();
// builder.Services.AddProblemDetails();
builder.AddDefaultProblemDetails();

var withApiVersioning = builder.Services.AddApiVersioning(options =>
{
    // Include "api-supported-versions" and "api-deprecated-versions" headers in all responses
    options.ReportApiVersions = true;
});

builder.AddDefaultOpenApi(withApiVersioning);

var app = builder.Build();

app.UseDefaultProblemDetails();

app.MapDefaultEndpoints();

// app.UseStatusCodePages(); // Included in UseDefaultProblemDetails().

// app.UseAuthentication();
// app.UseAuthorization();

app.MapCatalogApi();

app.UseDefaultOpenApi(); 

app.Run();

// For integration test
public partial class Program { }
