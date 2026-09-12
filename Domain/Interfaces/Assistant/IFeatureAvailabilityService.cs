// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Answers whether an optional feature exists on this installation, so anything that offers a page
/// gated by a feature guard asks the same question the Angular guard asks. Two kinds of feature are
/// resolved: a feature plugin, named exactly as its manifest and as the argument of its
/// featurePluginGuard, and the inbox, whose gate is the incoming mail server configuration. A name
/// that matches neither is refused (fail-closed) rather than assumed present, because a page nobody
/// can open is a smaller failure than a page Klacksy offers and the router then bounces.
/// </summary>
namespace Klacks.Api.Domain.Interfaces.Assistant;

public interface IFeatureAvailabilityService
{
    /// <param name="feature">Feature name from the page-key manifest (plugin name, or the inbox key)</param>
    /// <param name="cancellationToken">Cancels the settings lookup the inbox check performs</param>
    Task<bool> IsAvailableAsync(string feature, CancellationToken cancellationToken = default);
}
