using System.Diagnostics;
using System.Diagnostics.Metrics;

internal static class ActivityExtensions
{
    public const string ActivitySourceName = "OpenTelemetryLab";
    public const string MeterName = "OpenTelemetryLab";
    public static readonly ActivitySource ActivitySource = new(ActivitySourceName, "1.0.0");
    public static readonly Meter Meter = new(MeterName, "1.0.0");


    // See https://opentelemetry.io/docs/specs/otel/trace/semantic_conventions/exceptions/
    public static void SetExceptionTags(this Activity activity, Exception ex)
    {
        if (activity is null)
        {
            return;
        }

        activity.AddTag("exception.message", ex.Message);
        activity.AddTag("exception.stacktrace", ex.ToString());
        activity.AddTag("exception.type", ex.GetType().FullName);
        activity.SetStatus(ActivityStatusCode.Error);
    }

}
