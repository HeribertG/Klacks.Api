// Copyright (c) Heribert Gasparoli Private. All rights reserved.

namespace Klacks.Api.Domain.Interfaces.Assistant;

public interface IAgentTriggerEvent
{
    string Kind { get; }
    string Severity { get; }
    string Summary { get; }
    IReadOnlyDictionary<string, object?> Payload { get; }

    /// <summary>
    /// When set, the event is delivered only to this single user instead of being broadcast to
    /// all connected users. Null (the default) preserves the broadcast behaviour of domain triggers.
    /// </summary>
    Guid? TargetUserId => null;

    /// <summary>
    /// When true, the event reaches only users in a planning role (Admin or Authorised); regular
    /// employees never receive it. Operational alerts (hours drift, period close, unstaffed shift, ...)
    /// set this. Default false keeps companion-style triggers (curiosity, onboarding) broadcast to everyone.
    /// </summary>
    bool PlannersOnly => false;

    /// <summary>
    /// Interpolation values for an i18n <see cref="Summary"/> (a summary starting with
    /// <c>i18n:</c>). The frontend resolves the key in the user's UI language and substitutes these
    /// values. Null (the default) means the summary is plain text or needs no parameters.
    /// </summary>
    IReadOnlyDictionary<string, string>? SummaryParams => null;

    /// <summary>
    /// Stable content key used to deduplicate proactive notifications: the same key is delivered to a
    /// user at most once (persisted), so a recurring scan never re-sends the same alert. Defaults to
    /// the full Summary; events override it to ignore changing magnitudes (e.g. drift uses client+period).
    /// </summary>
    string DedupKey => Summary;
}
