// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Default IEffectiveTimeZoneResolver: a valid explicit override wins, otherwise the company's
/// configured zone is used. Consolidates a chain that used to be reimplemented per skill, one of which
/// (GetUserContextSkill) returned an unvalidated override as-is. The returned Id is always the IANA
/// form, even when the override or the company zone was resolved from a Windows time zone id.
/// @param companyClock - resolves the company's own configured time zone as the fallback
/// </summary>

using Klacks.Api.Domain.Interfaces.Assistant;
using Klacks.Api.Domain.Interfaces.Settings;
using Klacks.Api.Domain.Services.Settings;

namespace Klacks.Api.Domain.Services.Assistant.Skills;

public class EffectiveTimeZoneResolver : IEffectiveTimeZoneResolver
{
    private readonly ICompanyClock _companyClock;

    public EffectiveTimeZoneResolver(ICompanyClock companyClock)
    {
        _companyClock = companyClock;
    }

    public async Task<(TimeZoneInfo Zone, string Id)> ResolveAsync(
        string? userTimezone, CancellationToken cancellationToken = default)
    {
        if (!string.IsNullOrWhiteSpace(userTimezone)
            && TimeZoneInfo.TryFindSystemTimeZoneById(userTimezone.Trim(), out var explicitZone))
        {
            return (explicitZone, IanaTimeZoneId.From(explicitZone));
        }

        var companyZone = await _companyClock.GetTimeZoneAsync(cancellationToken);
        return (companyZone, IanaTimeZoneId.From(companyZone));
    }
}
