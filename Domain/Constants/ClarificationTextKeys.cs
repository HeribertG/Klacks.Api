// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Catalogue keys of the localized texts of the inbound clarification dialog: the ten planner notices,
/// the eight status words a planner notice can name, and the subject of the reply mail to the employee.
/// Every language pack ships all of them in assistant-texts.json (RequiredKeys is what the coverage guard
/// reads). The skill outputs handed to the language model are deliberately not part of the catalogue: the
/// model answers the user in the user's language.
/// </summary>

namespace Klacks.Api.Domain.Constants;

public static class ClarificationTextKeys
{
    private const string Prefix = "assistant.inboundClarification.";

    public const string PlannerStarted = Prefix + "planner.started";
    public const string PlannerShiftNone = Prefix + "planner.shiftNone";
    public const string PlannerAnswerContext = Prefix + "planner.answerContext";
    public const string PlannerAnswerUnclearNotice = Prefix + "planner.answerUnclearNotice";
    public const string PlannerExpired = Prefix + "planner.expired";
    public const string PlannerSendFailed = Prefix + "planner.sendFailed";
    public const string PlannerSuggested = Prefix + "planner.suggested";
    public const string PlannerNoPersonalTarget = Prefix + "planner.noPersonalTarget";
    public const string PlannerAnsweredAfterExpiry = Prefix + "planner.answeredAfterExpiry";
    public const string PlannerArrivedAfterClosure = Prefix + "planner.arrivedAfterClosure";

    public const string StatusOpen = Prefix + "status.open";
    public const string StatusAnswered = Prefix + "status.answered";
    public const string StatusUnresolved = Prefix + "status.unresolved";
    public const string StatusExpired = Prefix + "status.expired";
    public const string StatusTakenOver = Prefix + "status.takenOver";
    public const string StatusSuggested = Prefix + "status.suggested";
    public const string StatusUndelivered = Prefix + "status.undelivered";
    public const string StatusUnknown = Prefix + "status.unknown";

    public const string MailNeutralReplySubject = Prefix + "mail.neutralReplySubject";

    /// <summary>Every key a language pack has to ship. The coverage guard reads exactly this list.</summary>
    public static readonly IReadOnlyList<string> RequiredKeys =
    [
        PlannerStarted, PlannerShiftNone, PlannerAnswerContext, PlannerAnswerUnclearNotice, PlannerExpired,
        PlannerSendFailed, PlannerSuggested, PlannerNoPersonalTarget, PlannerAnsweredAfterExpiry,
        PlannerArrivedAfterClosure,
        StatusOpen, StatusAnswered, StatusUnresolved, StatusExpired, StatusTakenOver, StatusSuggested,
        StatusUndelivered, StatusUnknown,
        MailNeutralReplySubject
    ];
}
