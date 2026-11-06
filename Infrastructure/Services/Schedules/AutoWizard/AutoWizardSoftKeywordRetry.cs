// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

using Klacks.Api.Application.Constants;
using Klacks.Api.Application.DTOs.Schedules.AutoWizard;
using Klacks.Api.Application.DTOs.Schedules.Summary;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Application.Interfaces.Schedules;
using Klacks.Api.Application.Services.Schedules.AutoWizard;
using Klacks.Api.Domain.Constants;
using Klacks.ScheduleOptimizer.Models;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Klacks.Api.Infrastructure.Services.Schedules.AutoWizard;

/// <summary>
/// The AutoWizard's soft-keyword retry: after the strict run left slots open and the employees have restricting schedule
/// commands, the whole chain runs again from the same source in Soft keyword mode, under a budget of its own. Its final
/// scenario is marked Soft and offered only when it fills strictly more slots than the strict one; otherwise every
/// scenario it produced is removed. A failure, timeout or cancellation of the retry never touches the strict result.
/// </summary>
/// <param name="scopeFactory">Creates the scope of the restricting-command check and the name lookup.</param>
/// <param name="stageChain">Runs the three stages in Soft mode.</param>
/// <param name="maintenance">Summary, keyword-mode mark and removal of the retry's scenarios.</param>
/// <param name="logger">Logger of the orchestrator.</param>
internal sealed class AutoWizardSoftKeywordRetry
{
    private static readonly TimeSpan Budget = TimeSpan.FromMinutes(AutoWizardTimeBudget.SoftKeywordRetryMinutes);

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly AutoWizardStageChain _stageChain;
    private readonly AutoWizardScenarioMaintenance _maintenance;
    private readonly ILogger _logger;

    public AutoWizardSoftKeywordRetry(
        IServiceScopeFactory scopeFactory,
        AutoWizardStageChain stageChain,
        AutoWizardScenarioMaintenance maintenance,
        ILogger logger)
    {
        _scopeFactory = scopeFactory;
        _stageChain = stageChain;
        _maintenance = maintenance;
        _logger = logger;
    }

    /// <summary>Runs the retry when it is wanted and reports what became of it.</summary>
    /// <param name="jobId">The orchestrator job.</param>
    /// <param name="request">The start request of the strict run.</param>
    /// <param name="strictFinal">The strict run's final scenario.</param>
    /// <param name="strictSummary">Summary of the strict final scenario; null = no retry.</param>
    /// <param name="ct">Cancellation of the whole run; the retry links its own budget to it.</param>
    public async Task<AutoWizardSoftKeywordRetryResult> RunAsync(
        Guid jobId,
        StartAutoWizardRequest request,
        AutoWizardStageScenario strictFinal,
        ScenarioSummaryDto? strictSummary,
        CancellationToken ct)
    {
        if (!request.SoftKeywordRetry || request.KeywordMode != KeywordMode.Hard)
        {
            return AutoWizardSoftKeywordRetryResult.Of(AutoWizardSoftKeywordRetryOutcomes.NotRequested);
        }

        if (!AutoWizardSoftKeywordRetryPlanner.IsWanted(request, strictSummary))
        {
            return AutoWizardSoftKeywordRetryResult.Of(AutoWizardSoftKeywordRetryOutcomes.NotNeeded);
        }

        var softProduced = new List<AutoWizardStageScenario>();
        try
        {
            if (!await HasRestrictingWishesAsync(jobId, request, ct))
            {
                return AutoWizardSoftKeywordRetryResult.Of(AutoWizardSoftKeywordRetryOutcomes.NotNeeded);
            }

            using var softCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
            softCts.CancelAfter(Budget);
            return await RunSoftChainAsync(jobId, request, strictFinal, strictSummary!, softProduced, softCts.Token);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(
                ex,
                "AutoWizard job {JobId} - the soft-keyword retry failed, timed out or was cancelled; the strict scenario {ScenarioId} stays the only result",
                jobId, strictFinal.ScenarioId);
            await _maintenance.DeleteAsync(jobId, softProduced.Select(s => s.ScenarioId).ToList());
            return AutoWizardSoftKeywordRetryResult.Of(AutoWizardSoftKeywordRetryOutcomes.Failed);
        }
    }

    private async Task<AutoWizardSoftKeywordRetryResult> RunSoftChainAsync(
        Guid jobId,
        StartAutoWizardRequest request,
        AutoWizardStageScenario strictFinal,
        ScenarioSummaryDto strictSummary,
        List<AutoWizardStageScenario> softProduced,
        CancellationToken softCt)
    {
        _logger.LogInformation(
            "AutoWizard job {JobId} - strict scenario leaves {OpenSlots} slot(s) open; running the soft-keyword retry",
            jobId, strictSummary.OpenSlots);

        var chain = await _stageChain.RunAsync(jobId, request with { KeywordMode = KeywordMode.Soft }, softProduced, softCt);
        var softFinal = chain.FinalScenario;
        var softSummary = await _maintenance.BuildSummaryAsync(jobId, softFinal.Token, softCt);

        if (!AutoWizardSoftKeywordRetryPlanner.IsBetter(strictSummary, softSummary))
        {
            _logger.LogInformation(
                "AutoWizard job {JobId} - the soft-keyword scenario fills {SoftFilled} slot(s), the strict one {StrictFilled}; discarded",
                jobId, softSummary?.FilledSlots, strictSummary.FilledSlots);
            await _maintenance.DeleteAsync(jobId, softProduced.Select(s => s.ScenarioId).ToList());
            return AutoWizardSoftKeywordRetryResult.Of(AutoWizardSoftKeywordRetryOutcomes.NotBetter);
        }

        var names = await LoadNamesAsync(softSummary!, softCt);
        var alternative = AutoWizardSoftKeywordRetryPlanner.BuildAlternative(softFinal, strictSummary, softSummary!, names);
        await _maintenance.MarkKeywordModeAsync(
            softFinal.ScenarioId,
            KeywordMode.Soft,
            AutoWizardSoftKeywordRetryPlanner.BuildSoftScenarioNote(
                alternative.AdditionalFilledSlots, softSummary!.KeywordWishes, strictFinal.Name),
            softCt);

        await _maintenance.DeleteAsync(jobId, AutoWizardStageOutcomePlanner.ScenariosToDeleteOnSuccess(softProduced));

        _logger.LogInformation(
            "AutoWizard job {JobId} - soft-keyword scenario {ScenarioId} kept: {Additional} more slot(s), {Missed} missed wish(es)",
            jobId, softFinal.ScenarioId, alternative.AdditionalFilledSlots, alternative.BrokenWishes.Count);

        return new AutoWizardSoftKeywordRetryResult(AutoWizardSoftKeywordRetryOutcomes.Kept, alternative);
    }

    private async Task<bool> HasRestrictingWishesAsync(Guid jobId, StartAutoWizardRequest request, CancellationToken ct)
    {
        try
        {
            using var scope = _scopeFactory.CreateScope();
            var counter = scope.ServiceProvider.GetRequiredService<IKeywordWishCountService>();
            return await counter.HasRestrictingWishesAsync(
                request.AgentIds, request.PeriodFrom, request.PeriodUntil, request.AnalyseToken, ct);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogWarning(ex, "AutoWizard job {JobId} - the restricting schedule commands could not be read; no soft-keyword retry", jobId);
            return false;
        }
    }

    private async Task<IReadOnlyDictionary<Guid, string>> LoadNamesAsync(ScenarioSummaryDto softSummary, CancellationToken ct)
    {
        var ids = AutoWizardSoftKeywordRetryPlanner.MissedWishes(softSummary).Select(item => item.ClientId).Distinct().ToList();
        if (ids.Count == 0)
        {
            return new Dictionary<Guid, string>();
        }

        using var scope = _scopeFactory.CreateScope();
        return await scope.ServiceProvider.GetRequiredService<IClientRepository>().GetDisplayNamesAsync(ids, ct);
    }
}
