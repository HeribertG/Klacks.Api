// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Orchestrates the escalation chain's own state machine: resolve the roster, freeze it into stages,
/// and drive waves of notification through IEscalationNotifier. AdvanceAsync is the one method
/// called from two places - right after chain creation and again by the background sweep once a
/// stage's expiry wins - so a wave computed at 03:00 and a wave computed after A's 03:20 expiry run
/// through identical logic (docs/ENTWURF-eskalationskette-2026-08-16.md §5/§6).
/// </summary>
/// <param name="chainRepository">Persistence and the conditional-update surface for chains/stages.</param>
/// <param name="rosterService">Resolves the ordered call list for a chain's group.</param>
/// <param name="notifier">The narrow delivery path; never AgentTriggerService.</param>
/// <param name="settingsReader">Source for the three configurable time-budget caps.</param>
/// <param name="timeProvider">Injected clock so a test can advance time without waiting on it.</param>
/// <param name="logger">Logs an unreachable-roster or an already-resolved race, never throws out of a sweep tick.</param>

using Klacks.Api.Domain.Enums;
using Klacks.Api.Domain.Interfaces.Assistant;
using Klacks.Api.Domain.Interfaces.Settings;
using Klacks.Api.Domain.Models.Assistant.Escalation;
using Klacks.Api.Domain.Services.Assistant;

namespace Klacks.Api.Application.Services.Assistant.Escalation;

public class EscalationChainService : IEscalationChainService
{
    private const string ExhaustedNoRosterReason = "roster resolved empty or entirely unreachable";
    private const string ExhaustedDeadlineReason = "deadline passed without acknowledgement";
    private const int MaxAdvanceRounds = 64;

    private readonly IEscalationChainRepository _chainRepository;
    private readonly IEscalationRosterService _rosterService;
    private readonly IEscalationNotifier _notifier;
    private readonly ISettingsReader _settingsReader;
    private readonly TimeProvider _timeProvider;
    private readonly ILogger<EscalationChainService> _logger;

    public EscalationChainService(
        IEscalationChainRepository chainRepository,
        IEscalationRosterService rosterService,
        IEscalationNotifier notifier,
        ISettingsReader settingsReader,
        TimeProvider timeProvider,
        ILogger<EscalationChainService> logger)
    {
        _chainRepository = chainRepository;
        _rosterService = rosterService;
        _notifier = notifier;
        _settingsReader = settingsReader;
        _timeProvider = timeProvider;
        _logger = logger;
    }

    public async Task<Guid?> StartChainAsync(StartEscalationChainRequest request, CancellationToken cancellationToken = default)
    {
        var roster = await _rosterService.GetOrderedRosterAsync(request.GroupId, cancellationToken);
        var budget = await EscalationTimeBudgetReader.ReadAsync(_settingsReader, cancellationToken);

        var deadlineUtc = request.ShiftStartUtc - TimeSpan.FromHours(budget.PrepBufferHours);
        var now = _timeProvider.GetUtcNow().UtcDateTime;

        // The roster can only ever consume roster.Count x MaxStageMinutes of wall-clock time even run
        // fully serial. Beyond that window there is nothing useful a wave computation could do yet, so
        // starting now would just wake stage 1 with an artificial "reply in MaxStageMinutes" deadline
        // for a shift that may be weeks out. Skip silently; the caller can retry closer to the deadline.
        var reachableWindow = TimeSpan.FromMinutes(budget.MaxStageMinutes * Math.Max(roster.Count, 1));
        if (deadlineUtc - now > reachableWindow)
        {
            _logger.LogInformation(
                "Escalation chain not started for work {WorkId}: deadline {DeadlineUtc} is beyond the {RosterSize}-stage roster's reachable window ({ReachableWindow}); not urgent yet.",
                request.WorkId, deadlineUtc, roster.Count, reachableWindow);
            return null;
        }

        var chain = new EscalationChain
        {
            Id = Guid.NewGuid(),
            Status = EscalationChainStatus.Running,
            WorkId = request.WorkId,
            GroupId = request.GroupId,
            ShiftStartUtc = request.ShiftStartUtc,
            AbsentClientId = request.AbsentClientId,
            AbsentClientName = request.AbsentClientName,
            AbsenceBreakId = request.AbsenceBreakId,
            DeadlineUtc = deadlineUtc
        };

        var rank = 1;
        foreach (var candidate in roster)
        {
            chain.Stages.Add(new EscalationStage
            {
                Id = Guid.NewGuid(),
                EscalationChainId = chain.Id,
                Rank = rank++,
                UserId = candidate.UserId,
                UserDisplayName = candidate.DisplayName,
                Status = EscalationStageStatus.Pending
            });
        }

        var added = await _chainRepository.AddAsync(chain, cancellationToken);
        if (!added)
        {
            // A Running chain already exists for this WorkId (partial unique index) - e.g. CoverAbsence
            // was re-run on a shift that is already escalating. Leave the existing chain untouched.
            _logger.LogInformation(
                "Escalation chain not started for work {WorkId}: a chain is already running for this shift.",
                request.WorkId);
            return null;
        }

        if (chain.Stages.Count == 0)
        {
            _logger.LogWarning("Escalation chain {ChainId} started with an empty roster for group {GroupId}", chain.Id, request.GroupId);
            await _chainRepository.TryExhaustChainAsync(chain.Id, ExhaustedNoRosterReason, cancellationToken);
            return chain.Id;
        }

        await AdvanceAsync(chain.Id, cancellationToken);
        return chain.Id;
    }

    public async Task AdvanceAsync(Guid chainId, CancellationToken cancellationToken = default)
    {
        var budget = await EscalationTimeBudgetReader.ReadAsync(_settingsReader, cancellationToken);

        // Bounded loop, not recursion: each round either notifies at least one stage or permanently
        // skips one for lacking a delivery path (B2), so the pending count strictly shrinks. The
        // round cap is a defensive backstop, not a real limit any real roster reaches.
        for (var round = 0; round < MaxAdvanceRounds; round++)
        {
            var chain = await _chainRepository.GetByIdWithStagesAsync(chainId, cancellationToken);
            if (chain is null || chain.Status != EscalationChainStatus.Running)
            {
                return;
            }

            var pending = chain.Stages.Where(s => s.Status == EscalationStageStatus.Pending).OrderBy(s => s.Rank).ToList();
            var stillNotified = chain.Stages.Any(s => s.Status == EscalationStageStatus.Notified);

            if (pending.Count == 0)
            {
                if (!stillNotified)
                {
                    var nowUtc = _timeProvider.GetUtcNow().UtcDateTime;
                    var reason = nowUtc >= chain.DeadlineUtc ? ExhaustedDeadlineReason : ExhaustedNoRosterReason;
                    await _chainRepository.TryExhaustChainAsync(chainId, reason, cancellationToken);
                }

                return;
            }

            var now = _timeProvider.GetUtcNow().UtcDateTime;
            var wave = EscalationWaveCalculator.ComputeNextWave(
                now, chain.DeadlineUtc, pending.Count, budget.MinStageMinutes, budget.MaxStageMinutes);

            var waveStages = wave.IsParallel ? pending : pending.Take(1).ToList();
            var dueAtUtc = now + wave.Duration;
            var anyNotified = false;

            foreach (var stage in waveStages)
            {
                var result = await _notifier.NotifyStageAsync(chain, stage, dueAtUtc, cancellationToken);

                if (IsUndeliverable(result.Outcome))
                {
                    await _chainRepository.TrySkipStageAsync(stage.Id, $"delivery outcome: {result.Outcome}", cancellationToken);
                    continue;
                }

                var won = await _chainRepository.TryNotifyStageAsync(
                    stage.Id, now, dueAtUtc, result.Channel, result.Outcome.ToString(), result.DispatchRowId, cancellationToken);
                anyNotified = anyNotified || won;
            }

            if (anyNotified)
            {
                return;
            }

            // Every stage this round was undeliverable; loop again over the now-smaller pending set.
        }

        _logger.LogError("Escalation chain {ChainId} advance loop hit its round cap; leaving it Running for the next sweep tick", chainId);
    }

    public async Task<bool> AcknowledgeAsync(string userId, CancellationToken cancellationToken = default)
    {
        var stage = await _chainRepository.FindNotifiedStageForUserAsync(userId, cancellationToken);
        if (stage is null || stage.Chain is null)
        {
            return false;
        }

        var now = _timeProvider.GetUtcNow().UtcDateTime;
        var stageWon = await _chainRepository.TryAcknowledgeStageAsync(stage.Id, now, cancellationToken);
        if (!stageWon)
        {
            // Lost the race - the same stage already expired or was cancelled on another instance.
            return false;
        }

        var chainWon = await _chainRepository.TryAcknowledgeChainAsync(
            stage.EscalationChainId, userId, stage.UserDisplayName, now, cancellationToken);
        if (!chainWon)
        {
            // The chain itself was already resolved (e.g. Superseded by F3) while this reply was in
            // flight; the stage stays Acknowledged, which is still an honest record of what happened.
            return true;
        }

        var previouslyNotified = (await _chainRepository.GetStagesByChainAsync(stage.EscalationChainId, cancellationToken))
            .Where(s => s.Id != stage.Id && s.NotifiedAtUtc != null)
            .ToList();

        await _chainRepository.CancelRemainingStagesAsync(stage.EscalationChainId, stage.Id, cancellationToken);
        await _notifier.NotifyHandoffAsync(stage.Chain, stage, previouslyNotified, cancellationToken);

        return true;
    }

    public async Task<bool> CancelAsync(
        Guid chainId, string userId, string userName, string reason, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(reason))
        {
            throw new ArgumentException("A cancel reason is mandatory (decision B7).", nameof(reason));
        }

        var now = _timeProvider.GetUtcNow().UtcDateTime;
        var won = await _chainRepository.TryCancelChainAsync(chainId, userId, userName, reason, now, cancellationToken);
        if (won)
        {
            await _chainRepository.CancelRemainingStagesAsync(chainId, Guid.Empty, cancellationToken);
        }

        return won;
    }

    private static bool IsUndeliverable(OfflineMessengerDeliveryOutcome outcome) =>
        outcome is OfflineMessengerDeliveryOutcome.NoContact
            or OfflineMessengerDeliveryOutcome.Failed
            or OfflineMessengerDeliveryOutcome.ChannelUnavailable;
}
