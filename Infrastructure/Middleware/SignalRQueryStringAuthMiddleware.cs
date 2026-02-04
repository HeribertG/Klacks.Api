using System.IdentityModel.Tokens.Jwt;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using Klacks.Api.Domain.Models.Authentification;

namespace Klacks.Api.Infrastructure.Middleware;

/// <summary>
/// Middleware to extract and validate JWT token from SignalR query string.
/// This bypasses the normal JWT middleware for WebSocket connections.
/// </summary>
public class SignalRQueryStringAuthMiddleware
{
    private readonly RequestDelegate _next;
    private readonly IConfiguration _configuration;

    public SignalRQueryStringAuthMiddleware(RequestDelegate next, IConfiguration configuration)
    {
        _next = next;
        _configuration = configuration;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var path = context.Request.Path;

        // Only process SignalR hub requests
        if (path.StartsWithSegments("/hubs"))
        {
            var accessToken = context.Request.Query["access_token"].FirstOrDefault();

            if (!string.IsNullOrEmpty(accessToken))
            {
                // Decode the token (it might be URL-encoded)
                var decodedToken = Uri.UnescapeDataString(accessToken);

                // Validate the token and set the user
                var principal = ValidateToken(decodedToken);
                if (principal != null)
                {
                    context.User = principal;
                    Console.WriteLine($"[SignalR-Auth] User authenticated: {principal.Identity?.Name}");
                }
                else
                {
                    Console.WriteLine($"[SignalR-Auth] Token validation failed");
                }
            }
            else
            {
                Console.WriteLine($"[SignalR-Auth] No access_token found in query string");
            }
        }

        await _next(context);
    }

    private System.Security.Claims.ClaimsPrincipal? ValidateToken(string token)
    {
        try
        {
            var jwtSettings = new JwtSettings();
            _configuration.Bind(nameof(jwtSettings), jwtSettings);

            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.UTF8.GetBytes(jwtSettings.Secret);

            var validationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,
                ValidIssuer = jwtSettings.ValidIssuer,
                ValidAudience = jwtSettings.ValidAudience,
                IssuerSigningKey = new SymmetricSecurityKey(key),
                ClockSkew = TimeSpan.Zero
            };

            var principal = tokenHandler.ValidateToken(token, validationParameters, out _);
            return principal;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[SignalR-Auth] Token validation exception: {ex.Message}");
            return null;
        }
    }
}
