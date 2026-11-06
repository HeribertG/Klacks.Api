// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

using Klacks.Api.Application.Commands.AnalyseScenarios;
using Klacks.Api.Application.DTOs.Schedules.Summary;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Application.Interfaces.Schedules;
using Klacks.Api.Domain.Interfaces;
using Klacks.Api.Infrastructure.Mediator;
using Klacks.ScheduleOptimizer.Models;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Klacks.Api.Infrastructure.Services.Schedules.AutoWizard;

/// <summary>
/// The scenario bookkeeping of an AutoWizard run, each call in a scope of its own (the orchestrator is a singleton):
/// notes on a scenario's description, its keyword mode, the live summary and the removal of intermediate scenarios.
/// Notes, summary and removal are best-effort - they never turn a finished run into a failed one; marking the keyword
/// mode is not, a scenario that must carry Soft is never offered without it.
/// </summary>
/// <param name="scopeFactory">Creates the scope of each call.</param>
/// <param name="logger">Logger of the orchestrator.</param>
internal sealed class AutoWizardScenarioMaintenance
{
    private const string NoteSeparator = " | ";

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger _logger;

    public AutoWizardScenarioMaintenance(IServiceScopeFactory scopeFactory, ILogger logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    /// <summary>
    /// Appends the chain outcome to the scenario's description, best-effort. The terminal state expires after
    /// minutes and the chat history carries no job id, so the scenario is the durable place where the assistant
    /// and the operator read that a run failed or skipped its holistic harmonization.
    /// </summary>
    public async Task AnnotateAsync(Guid jobId, Guid scenarioId, string note)
    {
        try
        {
            using var scope = _scopeFactory.CreateScope();
            var repository = scope.ServiceProvider.GetRequiredService<IAnalyseScenarioRepository>();
            var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

            var scenario = await repository.Get(scenarioId);
            if (scenario == null)
            {
                return;
            }

            scenario.Description = AppendNote(scenario.Description, note);
            await repository.Put(scenario);
            await unitOfWork.CompleteAsync();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "AutoWizard job {JobId} could not note its outcome on scenario {ScenarioId}", jobId, scenarioId);
        }
    }

    /// <summary>
    /// Persists the keyword mode of the run on the scenario, optionally with a note. Throws when the scenario is gone or
    /// the write fails, so the caller never offers a soft plan that is not recognisable as one.
    /// </summary>
    public async Task MarkKeywordModeAsync(Guid scenarioId, KeywordMode keywordMode, string? note)
    {
        using var scope = _scopeFactory.CreateScope();
        var repository = scope.ServiceProvider.GetRequiredService<IAnalyseScenarioRepository>();
        var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

        var scenario = await repository.Get(scenarioId)
            ?? throw new InvalidOperationException($"Scenario {scenarioId} vanished before its keyword mode could be recorded.");

        scenario.SoftKeywordPlan = keywordMode == KeywordMode.Soft;
        if (note is not null)
        {
            scenario.Description = AppendNote(scenario.Description, note);
        }

        await repository.Put(scenario);
        await unitOfWork.CompleteAsync();
    }

    /// <summary>
    /// Live summary of a scenario for the completion payload. A failure here only drops the summary, never the finished chain.
    /// </summary>
    public async Task<ScenarioSummaryDto?> BuildSummaryAsync(Guid jobId, Guid scenarioToken, CancellationToken ct)
    {
        try
        {
            using var scope = _scopeFactory.CreateScope();
            var builder = scope.ServiceProvider.GetRequiredService<IScenarioSummaryBuilder>();
            return await builder.BuildAsync(scenarioToken, ct);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogWarning(ex, "AutoWizard job {JobId} - building the scenario summary failed", jobId);
            return null;
        }
    }

    /// <summary>
    /// Removes scenarios best-effort - a cleanup failure must never turn a finished or already reported run into
    /// something else. Runs with CancellationToken.None: after a cancel or the time budget the orchestrator token has
    /// fired, and a cancelled delete would leave the intermediates in every viewer's scenario list.
    /// </summary>
    public async Task DeleteAsync(Guid jobId, IReadOnlyList<Guid> scenarioIds)
    {
        if (scenarioIds.Count == 0)
        {
            return;
        }

        try
        {
            using var scope = _scopeFactory.CreateScope();
            var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
            foreach (var scenarioId in scenarioIds)
            {
                await mediator.Send(new DeleteAnalyseScenarioCommand(scenarioId), CancellationToken.None);
            }

            _logger.LogInformation(
                "AutoWizard job {JobId} removed {Count} intermediate scenario(s)", jobId, scenarioIds.Count);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "AutoWizard job {JobId} could not remove its intermediate scenarios", jobId);
        }
    }

    private static string AppendNote(string? description, string note)
        => string.IsNullOrWhiteSpace(description) ? note : $"{description}{NoteSeparator}{note}";
}
