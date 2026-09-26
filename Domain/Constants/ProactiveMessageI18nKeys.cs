// Copyright (c) Heribert Gasparoli Private. All rights reserved.

namespace Klacks.Api.Domain.Constants;

/// <summary>
/// i18n keys for proactive operational alerts. Each value is rendered by the frontend in the
/// connected user's UI language (the server does not know the user's language) and interpolated
/// with the event's SummaryParams. Keys must match the entries in the Klacks.Ui i18n files.
/// </summary>
public static class ProactiveMessageI18nKeys
{
    public const string TargetHoursDrift = "assistant.proactive.targetHoursDrift";

    /// <summary>
    /// The aggregated form of <see cref="TargetHoursDrift"/>: one sentence for the whole workforce of one
    /// period. Its parameters are count, period, hours (the largest absolute drift, signed) and names -
    /// deliberately no "name", because the aggregate names no single person. The per-employee key stays
    /// in every catalogue: dispatch rows written before the aggregation still reference it.
    /// </summary>
    public const string TargetHoursDriftSummary = "assistant.proactive.targetHoursDriftSummary";
    public const string PeriodCloseDue = "assistant.proactive.periodCloseDue";

    /// <summary>
    /// The form of <see cref="PeriodCloseDue"/> used when a close lag is stored: the period end and the close
    /// date are different days then, and the plain sentence ("ends on {date}") would present the close date
    /// as the period end. Parameters group, periodEnd, date (the close date) and days (until the close date).
    /// Without a lag the plain key keeps being sent unchanged.
    /// </summary>
    public const string PeriodCloseDueWithLag = "assistant.proactive.periodCloseDueWithLag";

    /// <summary>
    /// Klacksy sealed a group's period on its own (full autonomy). Parameters group, from and until.
    /// </summary>
    public const string PeriodAutoClosed = "assistant.proactive.periodAutoClosed";

    /// <summary>
    /// One key per cause for a period Klacksy was allowed to close but did not, all with the parameters group,
    /// from, until and errors (only meaningful for the errors key). Refused, Failed and NotVerified share the
    /// Failed sentence: for a planner all three mean "check the period and close it yourself if needed".
    /// </summary>
    public const string PeriodAutoCloseBlockedNoLag = "assistant.proactive.periodAutoCloseBlockedNoLag";
    public const string PeriodAutoCloseBlockedWindowMissed = "assistant.proactive.periodAutoCloseBlockedWindowMissed";
    public const string PeriodAutoCloseBlockedPartiallySealed = "assistant.proactive.periodAutoCloseBlockedPartiallySealed";
    public const string PeriodAutoCloseBlockedErrors = "assistant.proactive.periodAutoCloseBlockedErrors";
    public const string PeriodAutoCloseBlockedAutonomyLowered = "assistant.proactive.periodAutoCloseBlockedAutonomyLowered";
    public const string PeriodAutoCloseBlockedFailed = "assistant.proactive.periodAutoCloseBlockedFailed";
    public const string UnstaffedShift = "assistant.proactive.unstaffedShift";
    public const string LockConflict = "assistant.proactive.lockConflict";
    public const string ScenarioPending = "assistant.proactive.scenarioPending";
    public const string ContractExpiringSoon = "assistant.proactive.contractExpiringSoon";
    public const string WorkDroppedByErpImport = "assistant.proactive.workDroppedByErpImport";
    public const string OrderImportFailed = "assistant.proactive.orderImportFailed";
    public const string AvailabilityGap = "assistant.proactive.availabilityGap";

    /// <summary>
    /// The aggregated form of <see cref="AvailabilityGap"/>: one sentence for everybody who has not
    /// reported availability for one upcoming month. Its parameters are count, from, until and names -
    /// deliberately no "name", because the aggregate names no single person. The per-employee key stays
    /// in every catalogue: dispatch rows written before the aggregation still reference it.
    /// </summary>
    public const string AvailabilityGapSummary = "assistant.proactive.availabilityGapSummary";
    public const string PeriodOverdue = "assistant.proactive.periodOverdue";
    public const string ClientMissingAddress = "assistant.proactive.clientMissingAddress";
    public const string ClientMissingContact = "assistant.proactive.clientMissingContact";

    /// <summary>
    /// The aggregated forms of <see cref="ClientMissingAddress"/> and
    /// <see cref="ClientMissingContact"/>: one sentence per missing field for everybody lacking it,
    /// with the parameters count and names. Kept as two keys rather than one parameterised sentence
    /// because a missing address and a missing way to be contacted are different statements of fact
    /// and carry different severities. The per-employee keys stay in every catalogue: dispatch rows
    /// written before the aggregation still reference them.
    /// </summary>
    public const string ClientMissingAddressSummary = "assistant.proactive.clientMissingAddressSummary";
    public const string ClientMissingContactSummary = "assistant.proactive.clientMissingContactSummary";
    public const string MuteSuggestion = "assistant.proactive.muteSuggestion";
    public const string PlanPausedForApproval = "assistant.proactive.planPausedForApproval";
    public const string EscalationStageAlert = "assistant.proactive.escalationStageAlert";

    /// <summary>
    /// The wake-up sentence of a ProactiveApproval chain stage: which finding, what Klacksy would do about
    /// it, by when and where to approve. Its parameters are finding (the trigger kind), action (the
    /// remediation skill) and dueTime. Inbox and live push only - deliberately absent from
    /// MessengerProactiveTexts, because an approval request never goes out over the messenger.
    /// </summary>
    public const string EscalationApprovalRequest = "assistant.proactive.escalationApprovalRequest";
    public const string OpenOrder = "assistant.proactive.openOrder";
    public const string UncutFulldayShift = "assistant.proactive.uncutFulldayShift";
    public const string EmptyContainer = "assistant.proactive.emptyContainer";
    public const string DailyDigest = "assistant.proactive.dailyDigest";
    public const string ScenarioPrepared = "assistant.proactive.scenarioPrepared";
    public const string NextPeriodSchedulingDue = "assistant.proactive.nextPeriodSchedulingDue";
    public const string NextPeriodAutofillStarted = "assistant.proactive.nextPeriodAutofillStarted";
    public const string NextPeriodPlanCommitted = "assistant.proactive.nextPeriodPlanCommitted";
    public const string NextPeriodAutoCommitBlocked = "assistant.proactive.nextPeriodAutoCommitBlocked";

    /// <summary>
    /// One key per NextPeriodAutoCommitBlockReason other than NewViolations, which keeps the unsuffixed
    /// key above. Flat camelCase rather than a dotted suffix on that key: the Ui catalogue is a flat
    /// dictionary, and a key that is a strict prefix of another live key is the one shape whose lookup
    /// depends on the translation parser's traversal rules.
    /// </summary>
    public const string NextPeriodAutoCommitBlockedRefused = "assistant.proactive.nextPeriodAutoCommitBlockedRefused";
    public const string NextPeriodAutoCommitBlockedConflict = "assistant.proactive.nextPeriodAutoCommitBlockedConflict";
    public const string NextPeriodAutoCommitBlockedTimeout = "assistant.proactive.nextPeriodAutoCommitBlockedTimeout";
    public const string NextPeriodAutoCommitBlockedNotCommittable = "assistant.proactive.nextPeriodAutoCommitBlockedNotCommittable";
    public const string NextPeriodAutoCommitBlockedKillSwitch = "assistant.proactive.nextPeriodAutoCommitBlockedKillSwitch";
    public const string NextPeriodAutoCommitBlockedAutonomyLowered = "assistant.proactive.nextPeriodAutoCommitBlockedAutonomyLowered";
    public const string NextPeriodAutoCommitBlockedInterrupted = "assistant.proactive.nextPeriodAutoCommitBlockedInterrupted";
    public const string KlacksyLearnedDigest = "assistant.proactive.klacksyLearnedDigest";

    /// <summary>
    /// The turn-selection eval lost ground between two consecutive full runs. Its parameters name the
    /// goldset and the model the trend was measured on, both current values and both drops - a drop
    /// without the level it fell from tells an administrator nothing about how bad the state is.
    /// </summary>
    public const string EvalRegression = "assistant.proactive.evalRegression";
    public const string BulkSealOrdersCompleted = "assistant.proactive.bulkSealOrdersCompleted";
    public const string BulkSealOrdersFailed = "assistant.proactive.bulkSealOrdersFailed";

    /// <summary>
    /// Three mutually exclusive stages of an installation that has never been planned. Kept apart
    /// instead of one parameterised sentence because "no orders exist" and "orders exist but nothing
    /// is assigned" are different statements of fact — a single wording would be wrong in at least
    /// one of the cases, and the offer of help that follows it differs too.
    /// </summary>
    public const string SetupNothingYet = "assistant.proactive.setupNothingYet";
    public const string SetupOrdersButNoShifts = "assistant.proactive.setupOrdersButNoShifts";
    public const string SetupShiftsButNoWork = "assistant.proactive.setupShiftsButNoWork";

    /// <summary>
    /// A workforce of some size is already being planned, yet no group exists to scope, filter or
    /// hand over any part of it. Its only parameter is count, because the recommendation is about the
    /// SIZE of the workforce and names no single person. The sentence must stay an offer to show how
    /// the workforce could be organised and must never suggest that creating a group takes visibility
    /// away from anybody - GroupVisibilityPreservationService keeps the status quo when the first
    /// group appears, so such a claim would be factually wrong.
    /// </summary>
    public const string UngroupedWorkforce = "assistant.proactive.ungroupedWorkforce";

    /// <summary>
    /// Plannable shifts exist that no group owns. Its only parameter is count, because the
    /// recommendation is about how many shifts are concerned and names none of them. The sentence
    /// states only the two consequences that were verified in the code - such a shift is never sealed
    /// by the group-scoped period close (WorkRepository/SealedDayRepository) and appears in no
    /// per-group payroll export (PayrollExportDataLoader) - and must claim nothing about who can see
    /// the shift, because that depends on the show_ungrouped_shifts setting rather than on the
    /// missing membership alone.
    /// </summary>
    public const string UngroupedShifts = "assistant.proactive.ungroupedShifts";
}
