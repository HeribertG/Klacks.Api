// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Shared UTC normalisation and company-local conversion for the clarification dialog; Utc kept,
/// Unspecified treated as UTC, Local converted via ToUniversalTime.
/// </summary>

namespace Klacks.Api.Infrastructure.Inbound;

internal static class ClarificationTimeConversion
{
    internal static DateTime AsUtc(DateTime value) => value.Kind switch
    {
        DateTimeKind.Utc => value,
        DateTimeKind.Local => value.ToUniversalTime(),
        _ => DateTime.SpecifyKind(value, DateTimeKind.Utc)
    };

    internal static DateTime ToLocal(DateTime utc, TimeZoneInfo companyTimeZone) =>
        TimeZoneInfo.ConvertTimeFromUtc(AsUtc(utc), companyTimeZone);
}
