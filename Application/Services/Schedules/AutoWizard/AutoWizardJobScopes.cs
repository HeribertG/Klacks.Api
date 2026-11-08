// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

using System.Collections.Concurrent;
using Klacks.Api.Domain.Constants;

namespace Klacks.Api.Application.Services.Schedules.AutoWizard;

/// <summary>
/// In-memory record of the group every AutoWizard job plans for (the group its scenarios belong to) and of the employees
/// it plans, written when the job starts. AutoWizardJobAccessGuard reads it to decide which restricted callers may follow,
/// query or cancel a job. An entry is kept as long as the job can run plus as long as its terminal state stays readable, and is dropped after
/// that on the next start. A restart, or a status request that reaches another API instance, finds no entry; a restricted
/// caller then gets the answer for an unknown job (fail-closed), admins are not affected.
/// </summary>
/// <param name="timeProvider">Clock for the retention window</param>
public sealed class AutoWizardJobScopes
{
    private static readonly TimeSpan Retention = TimeSpan.FromMinutes(
        AutoWizardTimeBudget.TotalMinutes + JobTerminalStateDefaults.TtlMinutes);

    private readonly ConcurrentDictionary<Guid, AutoWizardJobScope> _scopes = new();
    private readonly TimeProvider _timeProvider;

    public AutoWizardJobScopes(TimeProvider timeProvider)
    {
        _timeProvider = timeProvider;
    }

    /// <summary>Records the group and the employees of a starting job and drops entries past their retention.</summary>
    /// <param name="jobId">The starting job</param>
    /// <param name="groupId">Group the job's scenarios belong to; null for a group-less run (only unrestricted callers start those)</param>
    /// <param name="agentIds">Employees the job plans</param>
    public void Record(Guid jobId, Guid? groupId, IReadOnlyList<Guid> agentIds)
    {
        var now = _timeProvider.GetUtcNow();
        foreach (var (id, scope) in _scopes)
        {
            if (now - scope.RecordedAt > Retention)
            {
                _scopes.TryRemove(id, out _);
            }
        }

        _scopes[jobId] = new AutoWizardJobScope(groupId, agentIds, now);
    }

    /// <summary>The recorded scope of a job, or null when the job is unknown here.</summary>
    /// <param name="jobId">The job asked about</param>
    public AutoWizardJobScope? Find(Guid jobId) => _scopes.TryGetValue(jobId, out var scope) ? scope : null;
}
