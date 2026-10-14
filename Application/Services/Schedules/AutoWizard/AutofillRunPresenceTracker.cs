// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

using System.Collections.Concurrent;
using Klacks.Api.Application.DTOs.Schedules.AutoWizard;
using Klacks.Api.Application.Interfaces.Schedules.AutoWizard;
using Microsoft.Extensions.Logging;

namespace Klacks.Api.Application.Services.Schedules.AutoWizard;

/// <summary>
/// Singleton in-memory record of the autofill runs in progress. The record changes first and the announcement follows,
/// so a viewer that asks right after an announcement already gets the new state; an announcement failure is logged and
/// swallowed because the run (and the release of its run lock) must never depend on it.
/// </summary>
/// <param name="notifier">Announces started and ended runs to the open schedules that may see them</param>
/// <param name="logger">Logs announcements that failed</param>
public sealed class AutofillRunPresenceTracker : IAutofillRunPresenceTracker
{
    private readonly ConcurrentDictionary<Guid, AutofillRunPresenceDto> _runs = new();
    private readonly IAutofillRunPresenceNotifier _notifier;
    private readonly ILogger<AutofillRunPresenceTracker> _logger;

    public AutofillRunPresenceTracker(
        IAutofillRunPresenceNotifier notifier,
        ILogger<AutofillRunPresenceTracker> logger)
    {
        _notifier = notifier;
        _logger = logger;
    }

    public async Task<AutofillRunPresenceDto> BeginAsync(Guid? groupId, DateOnly periodFrom, DateOnly periodUntil)
    {
        var run = new AutofillRunPresenceDto(Guid.NewGuid(), groupId, periodFrom, periodUntil, ProgressPercent: null);
        _runs[run.RunId] = run;

        try
        {
            await _notifier.NotifyStartedAsync(run);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Autofill presence - failed to announce the start of run {RunId}", run.RunId);
        }

        return run;
    }

    public async Task EndAsync(Guid runId)
    {
        if (!_runs.TryRemove(runId, out var run))
        {
            return;
        }

        try
        {
            await _notifier.NotifyEndedAsync(run);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Autofill presence - failed to announce the end of run {RunId}", runId);
        }
    }

    public IReadOnlyList<AutofillRunPresenceDto> GetActive() => _runs.Values.ToList();
}
