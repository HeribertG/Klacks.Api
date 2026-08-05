// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using System.Collections.Concurrent;
using System.Security.Cryptography;
using System.Text;
using Klacks.Api.Application.Constants;
using Klacks.Api.Application.Exceptions;
using Klacks.Api.Domain.Enums;

namespace Klacks.Api.Application.Services.Schedules;

/// <summary>
/// One place that decides whether an autofill run may start: it holds the size limits of every family
/// and refuses a second run of the identical selection. Both checks used to live in the controllers,
/// where each family had its own copy and the run lock did not exist at all - two planners could start
/// the same period at the same time, burn the time budget twice and then race for the apply.
/// The lock is deliberately in-memory and v1: a restart clears it (no job survives a restart either),
/// and it keys on the exact agent set rather than the group, because only one of the four request types
/// carries a group id at all. Slightly different selections on the same group therefore do not collide.
/// No TTL is needed - every runner cancels itself after its budget and releases in its finally block.
/// </summary>
public sealed class AutofillStartGuard
{
    private readonly ConcurrentDictionary<AutofillRunKey, Guid> _jobByKey = new();
    private readonly ConcurrentDictionary<Guid, AutofillRunKey> _keyByJob = new();

    /// <summary>
    /// Refuses a run that is larger than its family allows.
    /// </summary>
    /// <param name="family">Autofill family of the run.</param>
    /// <param name="agentCount">Number of agents in the selection.</param>
    /// <param name="shiftCount">Number of distinct shifts in the selection.</param>
    /// <param name="from">First day of the period, inclusive.</param>
    /// <param name="until">Last day of the period, inclusive.</param>
    public void EnsureWithinLimits(
        AutofillFamily family, int agentCount, int shiftCount, DateOnly from, DateOnly until)
    {
        var periodDays = Math.Max(1, until.DayNumber - from.DayNumber + 1);

        switch (family)
        {
            case AutofillFamily.Wizard1:
            case AutofillFamily.Benchmark:
                if (agentCount > WizardLimits.MaxAgents || shiftCount > WizardLimits.MaxShifts)
                {
                    throw new AutofillLimitExceededException(
                        WizardLimits.TooLargeErrorCode, family, agentCount, shiftCount, periodDays,
                        WizardLimits.MaxAgents, WizardLimits.MaxShifts, AutofillLimits.MaxPeriodDays);
                }

                break;

            case AutofillFamily.Harmonizer:
            case AutofillFamily.HolisticHarmonizer:
                if (agentCount > WizardLimits.MaxAgents || periodDays > AutofillLimits.MaxPeriodDays)
                {
                    var code = agentCount > WizardLimits.MaxAgents
                        ? WizardLimits.TooLargeErrorCode
                        : AutofillLimits.PeriodTooLongErrorCode;

                    throw new AutofillLimitExceededException(
                        code, family, agentCount, shiftCount, periodDays,
                        WizardLimits.MaxAgents, WizardLimits.MaxShifts, AutofillLimits.MaxPeriodDays);
                }

                break;

            case AutofillFamily.AutoWizard:
                var slotProduct = (long)agentCount * Math.Max(1, shiftCount) * periodDays;
                if (agentCount > AutoWizardLimits.MaxAgents
                    || shiftCount > AutoWizardLimits.MaxShifts
                    || slotProduct > AutoWizardLimits.MaxSlotProduct)
                {
                    throw new AutofillLimitExceededException(
                        AutoWizardLimits.TooLargeErrorCode, family, agentCount, shiftCount, periodDays,
                        AutoWizardLimits.MaxAgents, AutoWizardLimits.MaxShifts, AutofillLimits.MaxPeriodDays,
                        slotProduct, AutoWizardLimits.MaxSlotProduct);
                }

                break;
        }
    }

    /// <summary>
    /// Claims the run slot for the given selection, or refuses when an identical run is already going.
    /// </summary>
    /// <param name="family">Autofill family of the run.</param>
    /// <param name="from">First day of the period, inclusive.</param>
    /// <param name="until">Last day of the period, inclusive.</param>
    /// <param name="analyseToken">Scenario the run writes into; null is the main schedule.</param>
    /// <param name="agentIds">Selected agents; order does not matter.</param>
    /// <param name="jobId">Job that claims the slot.</param>
    public void AcquireRunLock(
        AutofillFamily family,
        DateOnly from,
        DateOnly until,
        Guid? analyseToken,
        IReadOnlyList<Guid> agentIds,
        Guid jobId)
    {
        var key = new AutofillRunKey(family, from, until, analyseToken ?? Guid.Empty, HashAgents(agentIds));

        while (!_jobByKey.TryAdd(key, jobId))
        {
            // A lost race against a release leaves no entry to report; try to claim it again.
            if (_jobByKey.TryGetValue(key, out var running))
            {
                throw new AutofillRunConflictException(running, family);
            }
        }

        _keyByJob[jobId] = key;
    }

    /// <summary>Releases the slot of a finished job; idempotent.</summary>
    /// <param name="jobId">Job whose slot is released.</param>
    public void ReleaseRunLock(Guid jobId)
    {
        if (!_keyByJob.TryRemove(jobId, out var key))
        {
            return;
        }

        _jobByKey.TryRemove(key, out _);
    }

    private static string HashAgents(IReadOnlyList<Guid> agentIds)
    {
        var joined = string.Join(",", agentIds.OrderBy(id => id));
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(joined)));
    }

    private sealed record AutofillRunKey(
        AutofillFamily Family, DateOnly From, DateOnly Until, Guid AnalyseToken, string AgentSetHash);
}
