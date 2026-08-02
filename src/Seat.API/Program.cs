
using ServiceDefaults;

var builder = WebApplication.CreateBuilder(args);

builder.AddBasicServiceDefaults();
builder.AddApplicationServices();
builder.Services.AddProblemDetails();

var withApiVersioning = builder.Services.AddApiVersioning(options =>
{
    // Include "api-supported-versions" and "api-deprecated-versions" headers in all responses
    options.ReportApiVersions = true;
});

builder.AddDefaultOpenApi(withApiVersioning);

builder.Services.AddGrpc();

var app = builder.Build();

app.MapDefaultEndpoints();

app.UseStatusCodePages();

// app.UseAuthentication();
// app.UseAuthorization();

app.MapSeatApi();

app.UseDefaultOpenApi();


app.MapGrpcService<SeatService>();

app.Run();

// for integration test
public partial class Program { }
