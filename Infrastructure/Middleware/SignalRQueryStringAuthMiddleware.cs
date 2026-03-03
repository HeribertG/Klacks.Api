// Copyright (c) Heribert Gasparoli Private. All rights reserved.

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
    private readonly ILogger<SignalRQueryStringAuthMiddleware> _logger;

    public SignalRQueryStringAuthMiddleware(RequestDelegate next, IConfiguration configuration, ILogger<SignalRQueryStringAuthMiddleware> logger)
    {
        _next = next;
        _configuration = configuration;
        _logger = logger;
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
                    _logger.LogDebug("SignalR-Auth: User authenticated: {UserName}", principal.Identity?.Name);
                }
                else
                {
                    _logger.LogWarning("SignalR-Auth: Token validation failed");
                }
            }
            else
            {
                _logger.LogDebug("SignalR-Auth: No access_token found in query string");
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
            _logger.LogWarning(ex, "SignalR-Auth: Token validation exception");
            return null;
        }
    }
}
