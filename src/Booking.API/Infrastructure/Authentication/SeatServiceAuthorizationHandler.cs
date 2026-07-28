using System.Net.Http.Headers;

namespace BookingService.API.Infrastructure.Authentication;

public sealed class SeatServiceAuthorizationHandler(
    SeatServiceTokenProvider tokenProvider)
    : DelegatingHandler
{
    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        var accessToken = await tokenProvider.GetAccessTokenAsync(cancellationToken);
        request.Headers.Authorization = new AuthenticationHeaderValue(
            "Bearer",
            accessToken);

        return await base.SendAsync(request, cancellationToken);
    }
}
