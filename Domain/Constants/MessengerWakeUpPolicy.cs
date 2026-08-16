// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Decides which proactive trigger events justify waking a human over the messenger channel.
/// This is the ONLY gate for the loud out-of-band channel; it deliberately does not touch the
/// SignalR live push, which keeps serving every companion event to users who are already
/// connected and therefore not asleep.
/// The rule is an allow-list ANDed with high severity, never an exclusion list: an event that is
/// forgotten here merely stays quiet and remains readable in the inbox, whereas an event that
/// slips into a loud default wakes every planner at night and costs the channel its acceptance.
/// Membership test applied to each kind: does a human acting tonight change the outcome?
/// The severity conjunction is what keeps a low-severity variant of an allow-listed kind quiet
/// (an unstaffed shift eight days out is not a night-time matter).
/// Per-recipient mute, snooze and minimum-severity are NOT re-checked here - AgentTriggerService
/// applies IAgentTriggerPreferenceService at the top of its recipient loop, so a muted recipient
/// never reaches this policy in the first place.
/// </summary>

namespace Klacks.Api.Domain.Constants;

public static class MessengerWakeUpPolicy
{
    /// <summary>
    /// Trigger kinds whose high-severity form is worth an out-of-band message at any hour.
    /// <list type="bullet">
    /// <item>UnstaffedShift: high means the shift starts within three days with nobody assigned;
    /// a planner who reacts now can still find a replacement.</item>
    /// <item>WorkDroppedByErpImport: an import silently superseded a planned assignment, so the
    /// published plan is wrong for an upcoming day until somebody restores it.</item>
    /// <item>OrderImportFailed: the ERP intake is broken and keeps rejecting orders for as long
    /// as it stays broken, so every hour of delay adds backlog.</item>
    /// </list>
    /// Deliberately excluded, with the reason each fails the "acting tonight changes the outcome"
    /// test: CuriosityQuestion, MuteSuggestion, SkillSequenceSuggestion and PlanPausedForApproval
    /// are companion chatter that waits without any cost; LockConflict asks for a review before the
    /// next wizard run, not tonight; ScenarioPending (high at seven days pending) and PeriodOverdue
    /// (high at twenty-one days overdue) have already been stale for weeks, so one more night is
    /// immaterial; PeriodCloseDue, ContractExpiringSoon, AvailabilityGap and TargetHoursDrift are
    /// office-hours administration that cannot be acted on at three in the morning.
    /// </summary>
    private static readonly IReadOnlySet<string> WakeUpKinds = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
        AgentTriggerKinds.UnstaffedShift,
        AgentTriggerKinds.WorkDroppedByErpImport,
        AgentTriggerKinds.OrderImportFailed
    };

    /// <summary>
    /// The complete wake-up list, exposed so a test can assert that MessengerProactiveTexts carries
    /// a sentence for every kind admitted here. Without that link a kind added to the list would go
    /// out as a raw translation key.
    /// </summary>
    public static IReadOnlySet<string> WakeUpTriggerKinds => WakeUpKinds;

    /// <summary>
    /// True when an event of this kind and severity may be pushed out over a messenger.
    /// </summary>
    /// <param name="triggerKind">Canonical trigger kind, see AgentTriggerKinds.</param>
    /// <param name="severity">Severity the event reports for this concrete occurrence.</param>
    public static bool JustifiesWakingSomebody(string triggerKind, string severity)
    {
        if (string.IsNullOrWhiteSpace(triggerKind))
        {
            return false;
        }

        return string.Equals(severity, AgentTriggerSeverity.High, StringComparison.OrdinalIgnoreCase)
            && WakeUpKinds.Contains(triggerKind);
    }
}
