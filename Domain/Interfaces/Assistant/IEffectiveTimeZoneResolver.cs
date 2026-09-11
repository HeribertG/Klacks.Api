// Copyright (c) Heribert Gasparoli Private. All rights reserved.

namespace Klacks.Api.Domain.Interfaces.Assistant;

/// <summary>
/// Resolves the single effective time zone for a skill call: an explicit, valid user override wins,
/// otherwise the company's own configured zone (via ICompanyClock) is used. Shared by every skill that
/// used to resolve this chain on its own, so an invalid or empty override cannot silently leak through
/// one call site while another validates it.
/// </summary>
public interface IEffectiveTimeZoneResolver
{
    /// <summary>
    /// Resolves the effective zone for a skill call.
    /// </summary>
    /// <param name="userTimezone">The caller's explicit IANA time zone override, or null/empty when
    /// none was set. Validated via <see cref="TimeZoneInfo.TryFindSystemTimeZoneById"/> - an invalid
    /// value falls through to the company zone exactly like an absent one.</param>
    Task<(TimeZoneInfo Zone, string Id)> ResolveAsync(string? userTimezone, CancellationToken cancellationToken = default);
}
