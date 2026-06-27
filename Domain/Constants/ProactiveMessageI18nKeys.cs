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
}
