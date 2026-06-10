// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Shared helper for the identity-provider skills: builds secret-free projections of an
/// <see cref="Klacks.Api.Application.DTOs.IdentityProviders.IdentityProviderResource"/>
/// (bind password and client secret are replaced by boolean presence flags) and validates
/// the port and provider-type parameters.
/// </summary>

using Klacks.Api.Application.DTOs.IdentityProviders;
using Klacks.Api.Domain.Enums;

namespace Klacks.Api.Application.Skills;

internal static class IdentityProviderSkillSupport
{
    internal const int MinPort = 1;
    internal const int MaxPort = 65535;

    public static object ToMaskedProjection(IdentityProviderResource resource)
    {
        return new
        {
            resource.Id,
            resource.Name,
            Type = resource.Type.ToString(),
            resource.IsEnabled,
            resource.SortOrder,
            resource.UseForAuthentication,
            resource.UseForClientImport,
            resource.Host,
            resource.Port,
            resource.UseSsl,
            resource.BaseDn,
            resource.BindDn,
            HasBindPassword = !string.IsNullOrEmpty(resource.BindPassword),
            resource.UserFilter,
            resource.ClientId,
            HasClientSecret = !string.IsNullOrEmpty(resource.ClientSecret),
            resource.AuthorizationUrl,
            resource.TokenUrl,
            resource.UserInfoUrl,
            resource.Scopes,
            resource.TenantId
        };
    }

    public static string? ValidatePort(int? port)
    {
        if (port.HasValue && (port.Value < MinPort || port.Value > MaxPort))
        {
            return $"Parameter 'port' must be between {MinPort} and {MaxPort}.";
        }

        return null;
    }

    public static string? TryParseType(string raw, out IdentityProviderType type)
    {
        if (!Enum.TryParse(raw.Trim(), true, out type) || !Enum.IsDefined(type))
        {
            return $"'{raw}' is not a valid identity provider type. Valid values: {string.Join(", ", Enum.GetNames<IdentityProviderType>())}.";
        }

        return null;
    }
}
