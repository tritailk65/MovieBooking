using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Options;

namespace BookingService.API.Infrastructure.Authentication;

public sealed class SeatServiceTokenProvider(
    HttpClient httpClient,
    IOptions<SeatServiceAuthenticationOptions> options)
{
    private readonly SemaphoreSlim _refreshLock = new(1, 1);
    private readonly SeatServiceAuthenticationOptions _options = options.Value;
    private string _accessToken = string.Empty;
    private DateTimeOffset _refreshAt;

    public async Task<string> GetAccessTokenAsync(CancellationToken cancellationToken)
    {
        if (HasValidToken())
        {
            return _accessToken;
        }

        await _refreshLock.WaitAsync(cancellationToken);
        try
        {
            if (HasValidToken())
            {
                return _accessToken;
            }

            using var request = new HttpRequestMessage(HttpMethod.Post, _options.TokenUrl);
            request.Headers.Authorization = new AuthenticationHeaderValue(
                "Basic",
                Convert.ToBase64String(
                    Encoding.UTF8.GetBytes(
                        $"{_options.ClientId}:{_options.ClientSecret}")));
            request.Content = new FormUrlEncodedContent(
                new Dictionary<string, string>
                {
                    ["grant_type"] = "client_credentials",
                    ["scope"] = _options.Scope
                });

            using var response = await httpClient.SendAsync(request, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync(cancellationToken);
                throw new HttpRequestException(
                    $"Cannot obtain a Seat service access token. " +
                    $"Token endpoint returned {(int)response.StatusCode}: {error}");
            }

            var token = await response.Content.ReadFromJsonAsync<TokenResponse>(
                cancellationToken: cancellationToken)
                ?? throw new HttpRequestException(
                    "The Seat service token endpoint returned an empty response.");

            _accessToken = token.AccessToken;
            _refreshAt = DateTimeOffset.UtcNow.AddSeconds(
                Math.Max(1, token.ExpiresIn - 30));

            return _accessToken;
        }
        finally
        {
            _refreshLock.Release();
        }
    }

    private bool HasValidToken()
        => _accessToken.Length > 0 && DateTimeOffset.UtcNow < _refreshAt;

    private sealed record TokenResponse(
        [property: JsonPropertyName("access_token")] string AccessToken,
        [property: JsonPropertyName("expires_in")] int ExpiresIn);
}
