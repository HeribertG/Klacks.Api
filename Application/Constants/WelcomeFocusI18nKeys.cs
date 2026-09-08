// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// i18n keys of the welcome focus question and its action button. The backend hands these out,
/// the frontend resolves them via TranslateService - no localized text lives in C# code. All
/// lists exactly the twelve keys this feature introduces, which is what the i18n gate test walks;
/// SetupConsultationAction deliberately stays out of it, because it is the pre-existing inbox
/// button key and is not owned by this feature.
/// </summary>

namespace Klacks.Api.Application.Constants;

public static class WelcomeFocusI18nKeys
{
    public const string KeyPrefix = "klacksy.focus";

    public const string NoOrdersPrompt = $"{KeyPrefix}.no-orders.prompt";
    public const string NoShiftsPrompt = $"{KeyPrefix}.no-shifts.prompt";
    public const string NoWorkPrompt = $"{KeyPrefix}.no-work.prompt";
    public const string PeriodOverduePrompt = $"{KeyPrefix}.period-overdue.prompt";
    public const string PeriodOverdueAction = $"{KeyPrefix}.period-overdue.action";
    public const string PeriodCloseDuePrompt = $"{KeyPrefix}.period-close-due.prompt";
    public const string PeriodCloseDueAction = $"{KeyPrefix}.period-close-due.action";
    public const string NextPeriodPrompt = $"{KeyPrefix}.next-period.prompt";
    public const string NextPeriodAction = $"{KeyPrefix}.next-period.action";
    public const string GenericPrompt = $"{KeyPrefix}.generic.prompt";
    public const string GenericAction = $"{KeyPrefix}.generic.action";
    public const string Later = $"{KeyPrefix}.later";

    public const string SetupConsultationAction = "setupConsultation.startButton";

    public static readonly IReadOnlyList<string> All =
    [
        NoOrdersPrompt,
        NoShiftsPrompt,
        NoWorkPrompt,
        PeriodOverduePrompt,
        PeriodOverdueAction,
        PeriodCloseDuePrompt,
        PeriodCloseDueAction,
        NextPeriodPrompt,
        NextPeriodAction,
        GenericPrompt,
        GenericAction,
        Later
    ];
}
