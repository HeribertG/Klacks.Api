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
    /// When true, the event reaches only users in the Admin role -- narrower than
    /// <see cref="PlannersOnly"/>. For alerts that are a data/integration concern (e.g. an ERP
    /// import failure) rather than a scheduling gap every planner should act on. Default false.
    /// </summary>
    bool AdminOnly => false;

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

    /// <summary>
    /// Frontend route the user can jump to in one click to act on this alert (e.g. the schedule or
    /// a client's edit page). Must be a route from navigation-targets.json. Null (the default) means
    /// the message carries no action.
    /// </summary>
    string? ActionRoute => null;

    /// <summary>
    /// Optional parameters accompanying <see cref="ActionRoute"/> (e.g. groupId, clientId, date) so
    /// the frontend can preselect the relevant context after navigating. Null when the route needs
    /// no parameters or <see cref="ActionRoute"/> is null.
    /// </summary>
    IReadOnlyDictionary<string, string>? ActionParams => null;

    /// <summary>
    /// Group this event concerns, if any. When set and <see cref="PlannersOnly"/> is true, the
    /// audience additionally narrows to planners whose GroupVisibility (including the group's whole
    /// Nested Set subtree) covers this group; Admins stay unrestricted. Null (the default) keeps the
    /// unscoped planner/admin broadcast unchanged.
    /// </summary>
    Guid? GroupId => null;
}
