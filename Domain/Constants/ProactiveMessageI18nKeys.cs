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
    public const string UnstaffedShift = "assistant.proactive.unstaffedShift";
    public const string LockConflict = "assistant.proactive.lockConflict";
    public const string ScenarioPending = "assistant.proactive.scenarioPending";
    public const string ContractExpiringSoon = "assistant.proactive.contractExpiringSoon";
    public const string WorkDroppedByErpImport = "assistant.proactive.workDroppedByErpImport";
    public const string OrderImportFailed = "assistant.proactive.orderImportFailed";
    public const string AvailabilityGap = "assistant.proactive.availabilityGap";
    public const string PeriodOverdue = "assistant.proactive.periodOverdue";
    public const string ClientMissingAddress = "assistant.proactive.clientMissingAddress";
    public const string ClientMissingContact = "assistant.proactive.clientMissingContact";
    public const string MuteSuggestion = "assistant.proactive.muteSuggestion";
    public const string PlanPausedForApproval = "assistant.proactive.planPausedForApproval";
    public const string EscalationStageAlert = "assistant.proactive.escalationStageAlert";
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
}
