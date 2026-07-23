// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Fails fast at startup when the configured JWT signing secret is missing, is a
/// committed placeholder, or is shorter than the minimum required key length.
/// Prevents a placeholder or weak secret from silently reaching a deployed environment.
/// </summary>
/// <param name="settings">The JWT settings bound from configuration whose Secret is validated.</param>
using System.Text;
using Klacks.Api.Domain.Models.Authentification;

namespace Klacks.Api.Infrastructure.StartupChecks;

public static class JwtSecretGuard
{
    public const int MinSecretByteLength = 32;

    public const string PlaceholderPrefix = "REPLACE_VIA";

    public static void Validate(JwtSettings settings)
    {
        ArgumentNullException.ThrowIfNull(settings);

        var secret = settings.Secret;

        if (string.IsNullOrWhiteSpace(secret))
        {
            throw new InvalidOperationException(
                "JWT secret is not configured. Set 'JwtSettings:Secret' via environment variable (JwtSettings__Secret) or user secrets.");
        }

        if (secret.StartsWith(PlaceholderPrefix, StringComparison.Ordinal))
        {
            throw new InvalidOperationException(
                "JWT secret is still set to the committed placeholder. Provide a real secret via environment variable (JwtSettings__Secret) or user secrets.");
        }

        var byteLength = Encoding.UTF8.GetByteCount(secret);
        if (byteLength < MinSecretByteLength)
        {
            throw new InvalidOperationException(
                $"JWT secret is too short ({byteLength} bytes). A minimum of {MinSecretByteLength} bytes (256 bit) is required.");
        }
    }
}
