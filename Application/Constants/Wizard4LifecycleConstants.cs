// Copyright (c) Heribert Gasparoli Private. All rights reserved.

namespace Klacks.Api.Application.Constants;

/// <summary>
/// Lifecycle facts of the background optimiser's candidates. A candidate is a suggestion nobody asked
/// for, so it must not pile up: a newer one for the same selection replaces the older, and one nobody
/// looked at expires by itself.
/// </summary>
public static class Wizard4LifecycleConstants
{
    /// <summary>Author recorded on every scenario the background optimiser creates.</summary>
    public const string SystemActor = "wizard4";

    /// <summary>Age after which an untouched candidate is removed.</summary>
    public static readonly TimeSpan CandidateTtl = TimeSpan.FromHours(48);

    /// <summary>A new candidate appeared.</summary>
    public const string ChangeKindCreated = "Created";

    /// <summary>A newer candidate replaced this one.</summary>
    public const string ChangeKindSuperseded = "Superseded";

    /// <summary>The candidate reached its time to live without being used.</summary>
    public const string ChangeKindExpired = "Expired";
}
