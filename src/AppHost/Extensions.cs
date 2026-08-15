using Aspire.Hosting.Eventing;
using Aspire.Hosting.Lifecycle;

namespace AppHost
{
    internal static class Extensions
    {
        /// <summary>
        /// Adds a hook to set the ASPNETCORE_FORWARDEDHEADERS_ENABLED environment variable to true for all projects in the application.
        /// </summary>
        public static IDistributedApplicationBuilder AddForwardedHeaders(this IDistributedApplicationBuilder builder)
        {
            builder.Services.TryAddEventingSubscriber<AddForwardHeadersSubscriber>();
            return builder;
        }

        //Thêm cấu hình forward header cho mỗi dịch vụ đứng sau 
        // Đem thông tin của header cho server vào dặn nó nhớ mỗi khi request được gửi tới thông qua Proxy
        private class AddForwardHeadersSubscriber : IDistributedApplicationEventingSubscriber
        {
            public Task SubscribeAsync(IDistributedApplicationEventing eventing, DistributedApplicationExecutionContext executionContext, CancellationToken cancellationToken)
            {
                eventing.Subscribe<BeforeStartEvent>((@event, ct) =>
                {
                    foreach (var p in @event.Model.GetProjectResources())
                    {
                        p.Annotations.Add(new EnvironmentCallbackAnnotation(context =>
                        {
                            context.EnvironmentVariables["ASPNETCORE_FORWARDEDHEADERS_ENABLED"] = "true";
                        }));
                    }

                    return Task.CompletedTask;
                });

                return Task.CompletedTask;
            }
        }
    }
}
