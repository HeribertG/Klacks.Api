// Copyright (c) Heribert Gasparoli Private. All rights reserved.

namespace Klacks.Api.Domain.Models.Assistant;

/// <summary>
/// Inputs the greeting composer needs to phrase one warm, grounded opening line.
/// </summary>
/// <param name="UserId">Authenticated user id, used only as a cache seed (one greeting per user/daypart/day).</param>
/// <param name="Language">UI language code (e.g. "de"); the greeting is written in this language.</param>
/// <param name="DisplayName">User's display name, or empty to greet without a name.</param>
/// <param name="Daypart">"morning" / "afternoon" / "evening".</param>
/// <param name="Latitude">Optional browser latitude; falls back to the company location when null.</param>
/// <param name="Longitude">Optional browser longitude; falls back to the company location when null.</param>
/// <param name="CountryCode">ISO country code for the public-holiday lookup.</param>
public sealed record GreetingContext(
    string UserId,
    string? Language,
    string DisplayName,
    string Daypart,
    double? Latitude,
    double? Longitude,
    string CountryCode);
