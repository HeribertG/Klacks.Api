using System.Net.Http.Headers;
using System.Text.Json;
using System.Web;
using Klacks.Api.Domain.Interfaces;
using Klacks.Api.Domain.Models.Authentification;

namespace Klacks.Api.Infrastructure.Services.Identity;

public class OAuth2Service : IOAuth2Service
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<OAuth2Service> _logger;
    private readonly HttpClient _insecureClient;

    public OAuth2Service(IHttpClientFactory httpClientFactory, ILogger<OAuth2Service> logger)
    {
        _httpClientFactory = httpClientFactory;
        _logger = logger;

        var handler = new HttpClientHandler
        {
            ServerCertificateCustomValidationCallback = (message, cert, chain, errors) => true
        };
        _insecureClient = new HttpClient(handler);
    }

    public string GetAuthorizationUrl(IdentityProvider provider, string redirectUri, string state)
    {
        if (string.IsNullOrEmpty(provider.AuthorizationUrl))
        {
            throw new InvalidOperationException("Authorization URL not configured");
        }

        var scopes = provider.Scopes ?? "openid email profile";

        var queryParams = HttpUtility.ParseQueryString(string.Empty);
        queryParams["client_id"] = provider.ClientId;
        queryParams["redirect_uri"] = redirectUri;
        queryParams["response_type"] = "code";
        queryParams["scope"] = scopes;
        queryParams["state"] = state;

        var authUrl = $"{provider.AuthorizationUrl}?{queryParams}";
        _logger.LogDebug("Generated OAuth2 authorization URL: {Url}", authUrl);

        return authUrl;
    }

    public async Task<OAuth2TokenResponse?> ExchangeCodeForTokenAsync(IdentityProvider provider, string code, string redirectUri)
    {
        if (string.IsNullOrEmpty(provider.TokenUrl))
        {
            throw new InvalidOperationException("Token URL not configured");
        }

        var requestBody = new Dictionary<string, string>
        {
            ["grant_type"] = "authorization_code",
            ["code"] = code,
            ["redirect_uri"] = redirectUri,
            ["client_id"] = provider.ClientId ?? string.Empty,
            ["client_secret"] = provider.ClientSecret ?? string.Empty
        };

        try
        {
            _logger.LogDebug("Exchanging authorization code for token at: {TokenUrl}", provider.TokenUrl);

            var response = await _insecureClient.PostAsync(provider.TokenUrl, new FormUrlEncodedContent(requestBody));
            var content = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError("Token exchange failed: {StatusCode} - {Content}", response.StatusCode, content);
                return null;
            }

            var tokenResponse = JsonSerializer.Deserialize<JsonElement>(content);

            return new OAuth2TokenResponse
            {
                AccessToken = tokenResponse.GetProperty("access_token").GetString() ?? string.Empty,
                RefreshToken = tokenResponse.TryGetProperty("refresh_token", out var rt) ? rt.GetString() : null,
                TokenType = tokenResponse.TryGetProperty("token_type", out var tt) ? tt.GetString() : null,
                ExpiresIn = tokenResponse.TryGetProperty("expires_in", out var ei) ? ei.GetInt32() : null,
                IdToken = tokenResponse.TryGetProperty("id_token", out var it) ? it.GetString() : null
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error exchanging authorization code for token");
            return null;
        }
    }

    public async Task<OAuth2UserInfo?> GetUserInfoAsync(IdentityProvider provider, string accessToken)
    {
        if (string.IsNullOrEmpty(provider.UserInfoUrl))
        {
            throw new InvalidOperationException("User info URL not configured");
        }

        _insecureClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

        try
        {
            _logger.LogDebug("Fetching user info from: {UserInfoUrl}", provider.UserInfoUrl);

            var response = await _insecureClient.GetAsync(provider.UserInfoUrl);
            var content = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError("User info request failed: {StatusCode} - {Content}", response.StatusCode, content);
                return null;
            }

            var userInfo = JsonSerializer.Deserialize<JsonElement>(content);

            return new OAuth2UserInfo
            {
                Id = GetStringProperty(userInfo, "sub", "id"),
                Email = GetStringProperty(userInfo, "email"),
                Name = GetStringProperty(userInfo, "name"),
                GivenName = GetStringProperty(userInfo, "given_name", "first_name"),
                FamilyName = GetStringProperty(userInfo, "family_name", "last_name"),
                Picture = GetStringProperty(userInfo, "picture", "avatar_url")
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching user info");
            return null;
        }
    }

    private static string? GetStringProperty(JsonElement element, params string[] propertyNames)
    {
        foreach (var name in propertyNames)
        {
            if (element.TryGetProperty(name, out var value) && value.ValueKind == JsonValueKind.String)
            {
                return value.GetString();
            }
        }
        return null;
    }
}
