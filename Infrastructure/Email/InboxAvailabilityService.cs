// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Mirrors the Angular InboxVisibilityService: the inbox exists once an incoming mail server, a user
/// name and a password are stored. The password is only checked for presence, never decrypted - an
/// installation whose DataProtection key is gone still has the page, it just cannot poll, and that is
/// a runtime failure rather than a missing feature.
/// </summary>
/// <param name="settingsReader">Read-only settings access supplying the three incoming-server keys</param>

using Klacks.Api.Application.Constants;
using Klacks.Api.Domain.Interfaces.Email;
using Klacks.Api.Domain.Interfaces.Settings;

namespace Klacks.Api.Infrastructure.Email;

public sealed class InboxAvailabilityService : IInboxAvailabilityService
{
    private static readonly string[] RequiredSettingKeys = InboxAvailabilitySettingKeys.All;

    private readonly ISettingsReader _settingsReader;

    public InboxAvailabilityService(ISettingsReader settingsReader)
    {
        _settingsReader = settingsReader;
    }

    public async Task<bool> IsAvailableAsync(CancellationToken cancellationToken = default)
    {
        var values = await _settingsReader.GetSettingsByTypesAsync(RequiredSettingKeys, cancellationToken);

        return RequiredSettingKeys.All(
            key => values.TryGetValue(key, out var value) && !string.IsNullOrWhiteSpace(value));
    }
}
