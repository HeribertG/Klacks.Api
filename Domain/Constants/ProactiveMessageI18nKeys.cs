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
    public const string KlacksyLearnedDigest = "assistant.proactive.klacksyLearnedDigest";
    public const string BulkSealOrdersCompleted = "assistant.proactive.bulkSealOrdersCompleted";
    public const string BulkSealOrdersFailed = "assistant.proactive.bulkSealOrdersFailed";
}
