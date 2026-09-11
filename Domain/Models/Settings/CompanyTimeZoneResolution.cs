// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// The company's resolved time zone together with which step of the ICompanyClock resolution chain
/// produced it (explicit setting, address country, global calendar country, or the neutral UTC
/// fallback), so a caller can tell a deliberately configured zone apart from an installation that
/// never configured one.
/// </summary>

using Klacks.Api.Domain.Enums;
using Klacks.Api.Domain.Services.Settings;

namespace Klacks.Api.Domain.Models.Settings;

public sealed class CompanyTimeZoneResolution
{
    public CompanyTimeZoneResolution(TimeZoneInfo zone, CompanyTimeZoneSource source)
    {
        Zone = zone;
        Source = source;
        IanaId = IanaTimeZoneId.From(zone);
    }

    public TimeZoneInfo Zone { get; }

    public CompanyTimeZoneSource Source { get; }

    /// <summary>
    /// The zone's IANA id - always this shape, even when Zone was resolved from a Windows time zone id,
    /// so every consumer (the company-clock endpoint, the ERP cron zone, scheduling skills) agrees.
    /// </summary>
    public string IanaId { get; }
}
