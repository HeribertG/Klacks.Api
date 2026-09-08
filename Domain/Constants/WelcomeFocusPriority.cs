// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Fixed ranking that decides which open finding becomes the welcome toast's single question.
/// Rank 0 is the most urgent. Every kind without an own entry shares GenericRank, so an unknown
/// or newly declared kind resolves instead of throwing. SeverityRank mirrors the database
/// ordering of AgentConditionRepository.GetOpenForScopeAsync (High before Medium before Low), so
/// the in-memory tie-break inside one rank cannot disagree with the query that produced the rows.
/// </summary>

namespace Klacks.Api.Domain.Constants;

public static class WelcomeFocusPriority
{
    public const int GenericRank = 4;

    private const int SetupRank = 0;
    private const int PeriodOverdueRank = 1;
    private const int PeriodCloseDueRank = 2;
    private const int NextPeriodRank = 3;

    private const int SeverityHighRank = 0;
    private const int SeverityMediumRank = 1;
    private const int SeverityLowRank = 2;

    private static readonly IReadOnlyDictionary<string, int> RankByKind = new Dictionary<string, int>(StringComparer.Ordinal)
    {
        [AgentTriggerKinds.NoScheduleYet] = SetupRank,
        [AgentTriggerKinds.PeriodOverdue] = PeriodOverdueRank,
        [AgentTriggerKinds.PeriodCloseDue] = PeriodCloseDueRank,
        [AgentTriggerKinds.NextPeriodSchedulingDue] = NextPeriodRank
    };

    public static int Rank(string triggerKind) =>
        RankByKind.TryGetValue(triggerKind, out var rank) ? rank : GenericRank;

    public static int SeverityRank(string severity) => severity switch
    {
        AgentTriggerSeverity.High => SeverityHighRank,
        AgentTriggerSeverity.Medium => SeverityMediumRank,
        _ => SeverityLowRank
    };
}
