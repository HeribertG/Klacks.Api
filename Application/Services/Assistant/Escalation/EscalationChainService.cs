// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Orchestrates the escalation chain's own state machine: resolve the roster, freeze it into stages,
/// and drive waves of notification through IEscalationNotifier. AdvanceAsync is the one method
/// called from two places - right after chain creation and again by the background sweep once a
/// stage's expiry wins - so a wave computed at 03:00 and a wave computed after A's 03:20 expiry run
/// through identical logic (docs/ENTWURF-eskalationskette-2026-08-16.md §5/§6). The two start paths
/// differ only in how the chain row and its roster come about: the absence path derives both here
/// (group roster, shift start minus prep buffer, urgency gate), the approval path receives both from
/// the caller and skips that arithmetic; from the AddAsync onwards they share one code path. They part
/// again at the acknowledgement: on a ProactiveApproval chain the acknowledgement IS the approval, so
/// once the chain-level compare-and-swap is won the approver is stamped onto the condition through the
/// ledger - never executed here, the tick does that under the stamped identity within its own window.
/// </summary>
/// <param name="chainRepository">Persistence and the conditional-update surface for chains/stages.</param>
/// <param name="rosterService">Resolves the ordered call list for an absence chain's group.</param>
/// <param name="notifier">The narrow delivery path; never AgentTriggerService.</param>
/// <param name="settingsReader">Source for the three configurable time-budget caps.</param>
/// <param name="ledgerService">Stamps the approval onto the condition once an approval chain is acknowledged.</param>
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

    /// <summary>
    /// How far past its own DeadlineUtc a chain may still be advanced. It covers clock skew between
    /// instances and the sweep's own tick boundary (BackgroundServiceOptions.EscalationChainSweepIntervalSeconds,
    /// 30s by default) and nothing more: inside it, B4's last-chance parallel wave still applies, beyond it
    /// the chain is stale and gets exhausted instead of waking its whole remaining roster at once.
    /// </summary>
    private const int DeadlineGraceMinutes = 1;

    private const string ApprovalNotStampedMessage =
        "Approval chain {ChainId} was acknowledged by {UserId} but condition {ConditionId} no longer accepts an "
        + "approval (moved on, or already approved); nothing will be executed for this acknowledgement";

    private const string ApproverNotAGuidMessage =
        "Approval chain {ChainId} was acknowledged by {UserId}, which is not a user id; no approval stamped";

    private const string ChainAlreadyResolvedMessage =
        "Escalation chain {ChainId} received an acknowledgement from {UserId} after the chain itself had already "
        + "been resolved; the stage keeps the reply as a record, but nothing was released by it";

    private readonly IEscalationChainRepository _chainRepository;
    private readonly IEscalationRosterService _rosterService;
    private readonly IEscalationNotifier _notifier;
    private readonly ISettingsReader _settingsReader;
    private readonly IAgentConditionLedgerService _ledgerService;
    private readonly TimeProvider _timeProvider;
    private readonly ILogger<EscalationChainService> _logger;

    public EscalationChainService(
        IEscalationChainRepository chainRepository,
        IEscalationRosterService rosterService,
        IEscalationNotifier notifier,
        ISettingsReader settingsReader,
        IAgentConditionLedgerService ledgerService,
        TimeProvider timeProvider,
        ILogger<EscalationChainService> logger)
    {
        _chainRepository = chainRepository;
        _rosterService = rosterService;
        _notifier = notifier;
        _settingsReader = settingsReader;
        _ledgerService = ledgerService;
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
            Purpose = EscalationChainPurpose.AbsenceCoverage,
            WorkId = request.WorkId,
            GroupId = request.GroupId,
            ShiftStartUtc = request.ShiftStartUtc,
            AbsentClientId = request.AbsentClientId,
            AbsentClientName = request.AbsentClientName,
            AbsenceBreakId = request.AbsenceBreakId,
            DeadlineUtc = deadlineUtc
        };

        return await CreateAndStartAsync(chain, roster, request.WorkId, cancellationToken);
    }

    public async Task<Guid?> StartConditionApprovalChainAsync(
        StartConditionApprovalChainRequest request, CancellationToken cancellationToken = default)
    {
        var chain = new EscalationChain
        {
            Id = Guid.NewGuid(),
            Status = EscalationChainStatus.Running,
            Purpose = EscalationChainPurpose.ProactiveApproval,
            ConditionId = request.ConditionId,
            GroupId = request.GroupId,
            DeadlineUtc = request.DeadlineUtc
        };

        return await CreateAndStartAsync(chain, request.Roster, request.ConditionId, cancellationToken);
    }

    private async Task<Guid?> CreateAndStartAsync(
        EscalationChain chain, IReadOnlyList<EscalationRosterCandidate> roster, Guid purposeKey, CancellationToken cancellationToken)
    {
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
            // A Running chain already exists for this purpose key (partial unique index) - e.g.
            // CoverAbsence was re-run on a shift that is already escalating, or the tick asked for an
            // approval a previous tick already asked for. Leave the existing chain untouched.
            _logger.LogInformation(
                "Escalation chain ({Purpose}) not started for key {PurposeKey}: a chain is already running for it.",
                chain.Purpose, purposeKey);
            return null;
        }

        if (chain.Stages.Count == 0)
        {
            _logger.LogWarning(
                "Escalation chain {ChainId} ({Purpose}) started with an empty roster for group {GroupId}",
                chain.Id, chain.Purpose, chain.GroupId);
            await ExhaustAsync(chain.Id, ExhaustedNoRosterReason, chain, cancellationToken);
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
                    await ExhaustAsync(chainId, reason, chain, cancellationToken);
                }

                return;
            }

            var now = _timeProvider.GetUtcNow().UtcDateTime;

            if (now > chain.DeadlineUtc.AddMinutes(DeadlineGraceMinutes))
            {
                await ExhaustAsync(chainId, ExhaustedDeadlineReason, chain, cancellationToken);
                return;
            }

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

    public async Task<EscalationAcknowledgeOutcome> AcknowledgeAsync(string userId, CancellationToken cancellationToken = default)
    {
        // Reply-path lookup: which chain isn't known in advance, so this takes whichever stage this
        // user currently holds Notified (most recent, if more than one - see FindNotifiedStageForUserAsync).
        var stage = await _chainRepository.FindNotifiedStageForUserAsync(userId, cancellationToken);
        if (stage is null || stage.Chain is null)
        {
            return EscalationAcknowledgeOutcome.NoNotifiedStage;
        }

        if (stage.Chain.Status != EscalationChainStatus.Running)
        {
            // A Notified stage under an already-resolved chain: impossible for chains created since the
            // exhaust started cancelling its stages, but rows left behind by the earlier behaviour still
            // look like this, and answering them must not read as an approval that took effect.
            _logger.LogInformation(ChainAlreadyResolvedMessage, stage.EscalationChainId, userId);
            return EscalationAcknowledgeOutcome.ChainAlreadyResolved;
        }

        return await AcknowledgeStageAsync(stage, cancellationToken);
    }

    public async Task<EscalationAcknowledgeOutcome> AcknowledgeChainAsync(
        Guid chainId, string userId, CancellationToken cancellationToken = default)
    {
        // UI intervention-list path: the chain is known, so this must resolve the stage WITHIN that
        // chain specifically - unlike AcknowledgeAsync, a user simultaneously Notified on more than
        // one chain must not have the wrong one resolved by this call.
        var chain = await _chainRepository.GetByIdWithStagesAsync(chainId, cancellationToken);
        if (chain is null)
        {
            return EscalationAcknowledgeOutcome.NoNotifiedStage;
        }

        if (chain.Status != EscalationChainStatus.Running)
        {
            // The chain ended before the click landed - the intervention row the user saw is stale. This is
            // checked before the stage lookup on purpose: the exhaust cancels the stages along with the
            // chain, so looking for a Notified stage first would report "nothing of yours here" and hide
            // the only fact the user needs, namely that the deadline is gone.
            _logger.LogInformation(ChainAlreadyResolvedMessage, chainId, userId);
            return EscalationAcknowledgeOutcome.ChainAlreadyResolved;
        }

        var stage = chain.Stages.FirstOrDefault(s => s.UserId == userId && s.Status == EscalationStageStatus.Notified);
        if (stage is null)
        {
            return EscalationAcknowledgeOutcome.NoNotifiedStage;
        }

        stage.Chain = chain;
        return await AcknowledgeStageAsync(stage, cancellationToken);
    }

    private async Task<EscalationAcknowledgeOutcome> AcknowledgeStageAsync(EscalationStage stage, CancellationToken cancellationToken)
    {
        var now = _timeProvider.GetUtcNow().UtcDateTime;
        var stageWon = await _chainRepository.TryAcknowledgeStageAsync(stage.Id, now, cancellationToken);
        if (!stageWon)
        {
            // Lost the race - the stage already expired, or an exhaust/cancel/supersede cancelled it
            // together with its chain. Either way this user no longer holds anything to acknowledge.
            return EscalationAcknowledgeOutcome.NoNotifiedStage;
        }

        var chainWon = await _chainRepository.TryAcknowledgeChainAsync(
            stage.EscalationChainId, stage.UserId, stage.UserDisplayName, now, cancellationToken);
        if (!chainWon)
        {
            // The chain itself was resolved between the stage guard and here. The stage stays
            // Acknowledged as an honest record of the reply, but NOTHING follows from it: no approval is
            // stamped, no handoff goes out. Reporting this as success is the bug this outcome replaces.
            _logger.LogInformation(ChainAlreadyResolvedMessage, stage.EscalationChainId, stage.UserId);
            return EscalationAcknowledgeOutcome.ChainAlreadyResolved;
        }

        await StampApprovalIfApprovalChainAsync(stage.Chain!, stage, cancellationToken);

        var previouslyNotified = (await _chainRepository.GetStagesByChainAsync(stage.EscalationChainId, cancellationToken))
            .Where(s => s.Id != stage.Id && s.NotifiedAtUtc != null)
            .ToList();

        await _chainRepository.CancelRemainingStagesAsync(stage.EscalationChainId, stage.Id, cancellationToken);
        await _notifier.NotifyHandoffAsync(stage.Chain!, stage, previouslyNotified, cancellationToken);

        return EscalationAcknowledgeOutcome.Acknowledged;
    }

    public Task<bool> ForceExhaustAsync(Guid chainId, string outcomeReason, CancellationToken cancellationToken = default) =>
        ExhaustAsync(chainId, outcomeReason, knownChain: null, cancellationToken);

    /// <summary>
    /// The single exhaust path: the repository ends the chain and cancels its remaining stages in one
    /// transaction, and whoever won that transition then closes the inbox rows of the stages that were
    /// still waiting. Doing it here rather than at each call site keeps the sweep's force-exhaust, the
    /// deadline guard, the no-roster case and the all-stages-resolved case on identical behaviour.
    /// </summary>
    private async Task<bool> ExhaustAsync(
        Guid chainId, string outcomeReason, EscalationChain? knownChain, CancellationToken cancellationToken)
    {
        var result = await _chainRepository.TryExhaustChainAsync(chainId, outcomeReason, cancellationToken);

        // The ?? guards the default struct a bare test double returns, whose list is null.
        var cancelledNotifiedStages = result.CancelledNotifiedStages ?? [];
        if (!result.Exhausted || cancelledNotifiedStages.Count == 0)
        {
            return result.Exhausted;
        }

        var chain = knownChain ?? await _chainRepository.GetByIdWithStagesAsync(chainId, cancellationToken);
        if (chain is null)
        {
            return true;
        }

        await _notifier.NotifyExhaustedAsync(chain, cancelledNotifiedStages, cancellationToken);
        return true;
    }

    /// <summary>
    /// The approval itself, taken only after the chain-level compare-and-swap was won, so at most one
    /// acknowledgement per chain ever reaches the ledger; the ledger's own guard (Reported, not yet
    /// approved) makes a replay across two chains a no-op too. A stamp that fails is logged, not thrown:
    /// the chain is honestly Acknowledged either way, and the finding has simply moved on.
    /// </summary>
    private async Task StampApprovalIfApprovalChainAsync(
        EscalationChain chain, EscalationStage stage, CancellationToken cancellationToken)
    {
        if (chain.Purpose != EscalationChainPurpose.ProactiveApproval || chain.ConditionId is not Guid conditionId)
        {
            return;
        }

        if (!Guid.TryParse(stage.UserId, out var approverUserId))
        {
            _logger.LogWarning(ApproverNotAGuidMessage, chain.Id, stage.UserId);
            return;
        }

        var stamped = await _ledgerService.TryApproveAsync(conditionId, approverUserId, cancellationToken);
        if (!stamped)
        {
            _logger.LogWarning(ApprovalNotStampedMessage, chain.Id, stage.UserId, conditionId);
        }
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

    public async Task<bool> SupersedeConditionApprovalChainAsync(
        Guid conditionId, string reason, CancellationToken cancellationToken = default)
    {
        var latest = await _chainRepository.GetLatestChainForConditionAsync(conditionId, cancellationToken);
        if (latest is null
            || latest.Status != EscalationChainStatus.Running
            || latest.Purpose != EscalationChainPurpose.ProactiveApproval)
        {
            return false;
        }

        if (!await _chainRepository.TrySupersedeChainAsync(latest.Id, reason, cancellationToken))
        {
            return false;
        }

        await _chainRepository.CancelRemainingStagesAsync(latest.Id, Guid.Empty, cancellationToken);
        return true;
    }

    private static bool IsUndeliverable(OfflineMessengerDeliveryOutcome outcome) =>
        outcome is OfflineMessengerDeliveryOutcome.NoContact
            or OfflineMessengerDeliveryOutcome.Failed
            or OfflineMessengerDeliveryOutcome.ChannelUnavailable;
}
