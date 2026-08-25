// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// The identity Klacksy acts under when it works on its own - no chat turn, no HTTP request, nobody
/// watching. Two things live here because an autonomous run needs both: the author string stamped on
/// what such a run creates, and the session-id convention marking a skill execution as one of those
/// runs. Both are read/write contracts rather than labels: a background path that writes one spelling
/// and queries another loses its own work, which is why Wizard4LifecycleConstants.SystemActor - the
/// same idea for the wizard's scenarios - is a constant and not a literal at each call site.
/// </summary>

namespace Klacks.Api.Domain.Constants;

public static class KlacksyIdentity
{
    /// <summary>
    /// Author recorded on rows an autonomous Klacksy run creates, above all
    /// AnalyseScenario.CreatedByUser. Without it the audit falls back to DataBaseContext's "Anonymous",
    /// because there is no NameIdentifier claim to read outside an HTTP request. Also the UserName of a
    /// proactive SkillExecutionContext, so the CurrentUserCreated a mutating skill stamps names Klacksy
    /// rather than the human whose rights the run borrowed.
    /// </summary>
    public const string SystemUserName = "klacksy";

    /// <summary>
    /// Prefix every proactive action's SessionId carries. It is what tells the execution log that a
    /// skill ran on Klacksy's own initiative rather than on somebody's request, mirroring the
    /// "scheduled-task:" convention ScheduledTaskRunner already uses for the unattended cron path.
    /// </summary>
    public const string ProactiveActionSessionPrefix = "proactive-action:";

    /// <summary>The SessionId a proactive action on this condition runs under.</summary>
    /// <param name="conditionId">Ledger row the action is remedying.</param>
    public static string ProactiveActionSessionId(Guid conditionId) =>
        ProactiveActionSessionPrefix + conditionId;
}
