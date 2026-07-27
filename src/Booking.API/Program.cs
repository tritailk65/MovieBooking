
var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();
builder.AddApplicationServices();
builder.Services.AddProblemDetails();

var withApiVersioning = builder.Services.AddApiVersioning(options =>
{
    // Include "api-supported-versions" and "api-deprecated-versions" headers in all responses
    options.ReportApiVersions = true;
});
builder.AddDefaultOpenApi(withApiVersioning);

var app = builder.Build();

app.MapDefaultEndpoints();

var booking = app.NewVersionedApi("Booking");

booking.MapBookingApiV1();

app.UseAuthentication();
app.UseAuthorization();

app.UseDefaultOpenApi();
app.Run();


// for integration test
public partial class Program { }