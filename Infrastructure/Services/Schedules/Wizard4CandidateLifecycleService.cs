// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Application.Constants;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Application.Interfaces.Schedules;
using Klacks.Api.Application.DTOs.Notifications;
using Klacks.Api.Domain.Interfaces;
using Klacks.Api.Domain.Models.Schedules;

namespace Klacks.Api.Infrastructure.Services.Schedules;

/// <summary>
/// Retires background-optimiser candidates. Retiring means the same thing the reject path means: the
/// cloned schedule data goes first, then the scenario is marked rejected - a scenario row without its
/// data would show up as an empty plan. Each retirement pushes a notification so an open scenario list
/// does not keep offering something that is gone.
/// </summary>
/// <param name="scenarioService">Removes the cloned schedule data of a scenario.</param>
/// <param name="repository">Scenario rows.</param>
/// <param name="unitOfWork">Commits the status change.</param>
/// <param name="notificationService">Pushes the change to the people viewing that period.</param>
/// <param name="logger">Logs retirements and failures.</param>
public sealed class Wizard4CandidateLifecycleService : IWizard4CandidateLifecycleService
{
    private readonly IAnalyseScenarioService _scenarioService;
    private readonly IAnalyseScenarioRepository _repository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IWorkNotificationService _notificationService;
    private readonly ILogger<Wizard4CandidateLifecycleService> _logger;

    public Wizard4CandidateLifecycleService(
        IAnalyseScenarioService scenarioService,
        IAnalyseScenarioRepository repository,
        IUnitOfWork unitOfWork,
        IWorkNotificationService notificationService,
        ILogger<Wizard4CandidateLifecycleService> logger)
    {
        _scenarioService = scenarioService;
        _repository = repository;
        _unitOfWork = unitOfWork;
        _notificationService = notificationService;
        _logger = logger;
    }

    public async Task SupersedeAsync(AnalyseScenario oldCandidate, CancellationToken ct)
    {
        await RetireAsync(oldCandidate, Wizard4LifecycleConstants.ChangeKindSuperseded, ct);
    }

    public async Task<int> ExpireStaleCandidatesAsync(DateTime nowUtc, CancellationToken ct)
    {
        var cutoff = nowUtc - Wizard4LifecycleConstants.CandidateTtl;
        var stale = await _repository.GetStaleCandidatesAsync(
            Wizard4LifecycleConstants.SystemActor, cutoff, ct);

        foreach (var candidate in stale)
        {
            ct.ThrowIfCancellationRequested();
            await RetireAsync(candidate, Wizard4LifecycleConstants.ChangeKindExpired, ct);
        }

        return stale.Count;
    }

    private async Task RetireAsync(AnalyseScenario candidate, string changeKind, CancellationToken ct)
    {
        await _scenarioService.SoftDeleteScenarioDataAsync(candidate.Token, ct);

        candidate.Status = Domain.Enums.AnalyseScenarioStatus.Rejected;
        await _repository.Put(candidate);
        await _unitOfWork.CompleteAsync();

        _logger.LogInformation(
            "Wizard4 candidate {ScenarioId} retired as {ChangeKind} (group {GroupId}, {From}..{Until}).",
            candidate.Id, changeKind, candidate.GroupId, candidate.FromDate, candidate.UntilDate);

        // Best effort: the candidate IS retired, and a failed push must not undo that. The list
        // recovers on the next load.
        try
        {
            await _notificationService.NotifyWizard4CandidatesChanged(new Wizard4CandidateNotificationDto(
                candidate.Id, candidate.GroupId, candidate.FromDate, candidate.UntilDate, changeKind));
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Wizard4 candidate {ScenarioId} retired but the push failed", candidate.Id);
        }
    }
}
