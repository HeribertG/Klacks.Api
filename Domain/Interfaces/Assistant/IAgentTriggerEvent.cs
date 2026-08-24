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
    /// The single domain entity this event is about (the shift, the client, ...), when there is one.
    /// The condition ledger stores it so a later remediation knows what to act on and so a cascade can
    /// be recognised by entity. Null (the default) means the event is not about one identifiable row;
    /// a ledger row then carries only its fingerprint.
    /// </summary>
    Guid? EntityId => null;

    /// <summary>
    /// Group this event concerns, if any. When set and <see cref="PlannersOnly"/> is true, the
    /// audience additionally narrows to planners whose GroupVisibility (including the group's whole
    /// Nested Set subtree) covers this group; Admins stay unrestricted. Null (the default) keeps the
    /// unscoped planner/admin broadcast unchanged. Only for events that concern exactly one group by
    /// construction (a period of one group, a scenario of one group); everything derived from a Shift
    /// must use <see cref="GroupIds"/> instead.
    /// </summary>
    Guid? GroupId => null;

    /// <summary>
    /// Every group this event concerns. A Shift is a member of MANY groups at once (GroupItem is a
    /// many-to-many join), so one <see cref="GroupId"/> cannot express the audience of a shift-borne
    /// finding: keeping only the first would deny the finding to the planners of every other group.
    /// When this is non-empty and <see cref="PlannersOnly"/> is true, the audience is the UNION of the
    /// group-scoped planner audiences of all of them. The default derives the collection from the
    /// single <see cref="GroupId"/>, so an event that genuinely concerns exactly one group only has to
    /// set that one and keeps its previous behaviour unchanged.
    /// </summary>
    IReadOnlyCollection<Guid> GroupIds => GroupId is Guid groupId ? new[] { groupId } : Array.Empty<Guid>();

    /// <summary>
    /// Declares that this event is only ever about a group-owned entity, so an empty
    /// <see cref="GroupIds"/> means "the group could not be determined" and never "this concerns
    /// everybody". Such an event then reaches Admins only instead of the unscoped planner broadcast.
    /// Default false keeps genuinely installation-wide alerts (hours drift, expiring contract,
    /// missing client core data) broadcast to every planner as before.
    /// </summary>
    bool RequiresGroupScope => false;
}
