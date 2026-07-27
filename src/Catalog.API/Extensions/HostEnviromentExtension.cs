using System.Reflection;

namespace Catalog.API.Extensions
{
    internal static class HostEnviromentExtension
    {
        public static bool IsBuild(this IHostEnvironment hostEnvironment)
        {
            // Check if the environment is "Build" or the entry assembly is "GetDocument.Insider"
            // to account for scenarios where app is launching via OpenAPI build-time generation
            // via the GetDocument.Insider tool.
            return hostEnvironment.IsEnvironment("Build") || Assembly.GetEntryAssembly()?.GetName().Name == "GetDocument.Insider";
        }
    }
}
