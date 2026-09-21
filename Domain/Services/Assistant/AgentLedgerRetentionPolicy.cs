// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Decides which finished ledger rows are old enough to be soft-deleted. Expressed as EF-translatable
/// predicates so the repository runs them inside the database while a unit test compiles the very same
/// expression and runs it against plain objects. A row's age is measured from the stamp of the event that
/// finished it (ResolvedAtUtc, HandledAtUtc, EscalatedAtUtc) and falls back to LastSeenAtUtc when that
/// stamp was never written; open rows are never eligible.
/// </summary>

using System.Linq.Expressions;
using Klacks.Api.Domain.Constants;
using Klacks.Api.Domain.Enums;
using Klacks.Api.Domain.Models.Assistant;

namespace Klacks.Api.Domain.Services.Assistant;

public static class AgentLedgerRetentionPolicy
{
    public static DateTime ShortLivedConditionCutoff(DateTime nowUtc) =>
        nowUtc.AddDays(-AgentLedgerRetentionDefaults.ResolvedOrRejectedConditionDays);

    public static DateTime LongLivedConditionCutoff(DateTime nowUtc) =>
        nowUtc.AddDays(-AgentLedgerRetentionDefaults.ExecutedOrEscalatedConditionDays);

    public static DateTime DispatchCutoff(DateTime nowUtc) =>
        nowUtc.AddDays(-AgentLedgerRetentionDefaults.DispatchDays);

    /// <param name="shortLivedCutoffUtc">Rows of status Resolved/Rejected finished before this are eligible.</param>
    /// <param name="longLivedCutoffUtc">Rows of status Executed/Escalated finished before this are eligible.</param>
    public static Expression<Func<AgentCondition, bool>> ConditionEligible(
        DateTime shortLivedCutoffUtc, DateTime longLivedCutoffUtc) =>
        c => (c.Status == AgentConditionStatus.Resolved
                && (c.ResolvedAtUtc ?? c.LastSeenAtUtc) < shortLivedCutoffUtc)
            || (c.Status == AgentConditionStatus.Rejected
                && (c.HandledAtUtc ?? c.LastSeenAtUtc) < shortLivedCutoffUtc)
            || (c.Status == AgentConditionStatus.Executed
                && (c.HandledAtUtc ?? c.LastSeenAtUtc) < longLivedCutoffUtc)
            || (c.Status == AgentConditionStatus.Escalated
                && (c.EscalatedAtUtc ?? c.LastSeenAtUtc) < longLivedCutoffUtc);

    /// <param name="cutoffUtc">Rows whose creation, last read and last reaction all precede this are eligible.</param>
    public static Expression<Func<ProactiveTriggerDispatchRow, bool>> DispatchEligible(DateTime cutoffUtc) =>
        d => d.CreateTime < cutoffUtc
            && (d.ReadAtUtc ?? d.CreateTime) < cutoffUtc
            && (d.ReactionAtUtc ?? d.CreateTime) < cutoffUtc;
}
