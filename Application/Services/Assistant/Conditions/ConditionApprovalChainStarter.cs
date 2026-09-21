// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Default <see cref="IConditionApprovalChainStarter"/>. Walks the start rules in the order that costs
/// least and fails closed at every step: the latest chain for the condition is consulted first (Running
/// means the question is already out; created on the current company day means it was asked today and
/// must not be asked again before the next one - Owner decision 2026-09-20), then the remediation skill's
/// RequiredPermissions are read off the live registry, the roster is resolved and rights-filtered by
/// IConditionApprovalRosterResolver, the deadline is one approval window per stage
/// (ProactiveApprovalDeadline over PROACTIVE_APPROVAL_WINDOW_MINUTES), and only then is the chain started.
/// An unknown skill, an empty roster and a declined start all end without a chain and are logged at
/// Information - none of them is an error of this tick, and none of them is retried within it.
/// </summary>
/// <param name="chainRepository">Latest chain per condition, for the two "not now" rules.</param>
/// <param name="chainService">Starts the ProactiveApproval chain from the resolved roster and deadline.</param>
/// <param name="rosterResolver">Ordered, rights-filtered approval candidates for the condition.</param>
/// <param name="skillRegistry">Source of the remediation skill's RequiredPermissions.</param>
/// <param name="settingsReader">Source of the per-stage approval window.</param>
/// <param name="timeProvider">Clock the deadline is computed from.</param>
/// <param name="logger">Records every start that did not happen, and why.</param>

using Klacks.Api.Domain.Enums;
using Klacks.Api.Domain.Interfaces.Assistant;
using Klacks.Api.Domain.Interfaces.Settings;
using Klacks.Api.Domain.Models.Assistant;
using Klacks.Api.Domain.Services.Assistant;
using Klacks.Api.Application.Services.Assistant.Escalation;

namespace Klacks.Api.Application.Services.Assistant.Conditions;

public sealed class ConditionApprovalChainStarter : IConditionApprovalChainStarter
{
    private const string ChainRunningMessage =
        "Condition {ConditionId} ({Kind}) already has approval chain {ChainId} running; not asking again";

    private const string ChainEndedTodayMessage =
        "Condition {ConditionId} ({Kind}) had approval chain {ChainId} end {Status} on the current company day; "
        + "a new chain is not started before the next one";

    private const string UnknownSkillMessage =
        "Condition {ConditionId} ({Kind}) names remediation skill {Skill}, which the skill registry does not know; "
        + "no approval chain is started";

    private const string NoApproverMessage =
        "Condition {ConditionId} ({Kind}) has no eligible approver for {Skill}: nobody in the roster holds its "
        + "permissions; the finding stays reported and unhandled";

    private const string NotStartedMessage =
        "Condition {ConditionId} ({Kind}): the approval chain was not started (another instance holds it); "
        + "the finding stays reported for now";

    private const string StartedMessage =
        "Condition {ConditionId} ({Kind}): approval chain {ChainId} started with {Stages} stage(s), deadline {DeadlineUtc}";

    private readonly IEscalationChainRepository _chainRepository;
    private readonly IEscalationChainService _chainService;
    private readonly IConditionApprovalRosterResolver _rosterResolver;
    private readonly ISkillRegistry _skillRegistry;
    private readonly ISettingsReader _settingsReader;
    private readonly TimeProvider _timeProvider;
    private readonly ILogger<ConditionApprovalChainStarter> _logger;

    public ConditionApprovalChainStarter(
        IEscalationChainRepository chainRepository,
        IEscalationChainService chainService,
        IConditionApprovalRosterResolver rosterResolver,
        ISkillRegistry skillRegistry,
        ISettingsReader settingsReader,
        TimeProvider timeProvider,
        ILogger<ConditionApprovalChainStarter> logger)
    {
        _chainRepository = chainRepository;
        _chainService = chainService;
        _rosterResolver = rosterResolver;
        _skillRegistry = skillRegistry;
        _settingsReader = settingsReader;
        _timeProvider = timeProvider;
        _logger = logger;
    }

    public async Task<ConditionApprovalStartOutcome> TryStartAsync(
        AgentCondition condition,
        ConditionRemediationEntry entry,
        DateTime companyDayStartUtc,
        CancellationToken cancellationToken = default)
    {
        var latest = await _chainRepository.GetLatestChainForConditionAsync(condition.Id, cancellationToken);
        if (latest is not null)
        {
            if (latest.Status == EscalationChainStatus.Running)
            {
                _logger.LogInformation(ChainRunningMessage, condition.Id, condition.TriggerKind, latest.Id);
                return ConditionApprovalStartOutcome.ChainAlreadyRunning;
            }

            if (latest.CreateTime is { } createdUtc && createdUtc >= companyDayStartUtc)
            {
                _logger.LogInformation(
                    ChainEndedTodayMessage, condition.Id, condition.TriggerKind, latest.Id, latest.Status);
                return ConditionApprovalStartOutcome.WaitingForNextCompanyDay;
            }
        }

        var descriptor = _skillRegistry.GetSkillByName(entry.RemediationSkillName);
        if (descriptor is null)
        {
            _logger.LogInformation(UnknownSkillMessage, condition.Id, condition.TriggerKind, entry.RemediationSkillName);
            return ConditionApprovalStartOutcome.NoEligibleApprover;
        }

        var roster = await _rosterResolver.ResolveAsync(condition, descriptor.RequiredPermissions, cancellationToken);
        if (roster.Count == 0)
        {
            _logger.LogInformation(NoApproverMessage, condition.Id, condition.TriggerKind, entry.RemediationSkillName);
            return ConditionApprovalStartOutcome.NoEligibleApprover;
        }

        var windowMinutes = await ProactiveApprovalWindowReader.ReadMinutesAsync(_settingsReader, cancellationToken);
        var deadlineUtc = ProactiveApprovalDeadline.Compute(
            _timeProvider.GetUtcNow().UtcDateTime, roster.Count, windowMinutes);

        var chainId = await _chainService.StartConditionApprovalChainAsync(
            new StartConditionApprovalChainRequest(condition.Id, condition.GroupId, roster, deadlineUtc),
            cancellationToken);

        if (chainId is not Guid startedChainId)
        {
            _logger.LogInformation(NotStartedMessage, condition.Id, condition.TriggerKind);
            return ConditionApprovalStartOutcome.NotStarted;
        }

        _logger.LogInformation(
            StartedMessage, condition.Id, condition.TriggerKind, startedChainId, roster.Count, deadlineUtc);
        return ConditionApprovalStartOutcome.Started;
    }
}
