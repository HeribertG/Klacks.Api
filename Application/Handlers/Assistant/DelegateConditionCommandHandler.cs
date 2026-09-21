// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Grants a single-condition "mach du" delegation (Etappe 4e) - and, since the approval chain (design
/// 2026-09-20), records the delegating planner as the finding's APPROVER. Three gates run before the
/// ledger is touched: a visibility gate (is the delegating user a planner at all, and is this specific
/// condition within their own group scope - answered NotFound either way, so an out-of-scope condition
/// is never revealed to exist) and a rights gate (does the person hold the remediation skill's own
/// RequiredPermissions, Admin bypass, through the same ISkillPermissionGate the approval roster is
/// filtered with - answered Forbidden with the reason, because by then the condition is already known to
/// be theirs to see). A kind without a registered remediation has nothing to hand over and is refused
/// the same way rather than storing a grant nothing could ever act on.
///
/// Delegating a Reported row IS the approval: the row gets ApprovedByUserId/ApprovedAtUtc stamped for the
/// delegating user, so the next tick executes under their rights exactly as after a chain acknowledgement,
/// and a ProactiveApproval chain still Running for the row is superseded so its roster is no longer woken.
/// A Prepared or Escalated row keeps the plain cap-raise semantics: the grant is stored, nothing is stamped.
/// </summary>
/// <param name="dispatchRepository">Resolves the message id to the dispatch row it targets, and to the condition it reported.</param>
/// <param name="scopeResolver">Answers whether the delegating user may even see this condition.</param>
/// <param name="conditionRepository">Scoped existence check for the condition (Etappe 4e never trusts an unscoped id lookup).</param>
/// <param name="ledgerService">Writes the grant and, for a Reported row, the approval stamp onto the ledger row.</param>
/// <param name="remediationRegistry">Which skill remediates the condition's kind; its permissions are what the rights gate checks.</param>
/// <param name="skillRegistry">Source of that skill's RequiredPermissions.</param>
/// <param name="permissionGate">Whether the delegating user's current rights cover them.</param>
/// <param name="chainService">Supersedes a Running approval chain the delegation has just answered.</param>
/// <param name="logger">Logs a failed best-effort acknowledgement or a stamp lost to a concurrent approval, without failing the delegation.</param>

using System.Globalization;
using Klacks.Api.Application.Commands.Assistant;
using Klacks.Api.Domain.Enums;
using Klacks.Api.Domain.Interfaces.Assistant;
using Klacks.Api.Domain.Models.Assistant;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Handlers.Assistant;

public class DelegateConditionCommandHandler : IRequestHandler<DelegateConditionCommand, DelegateConditionResult>
{
    private const string NoRemediationReason =
        "Klacksy has no remediation for this kind of finding, so there is nothing to hand over.";

    private const string UnknownSkillReasonFormat =
        "The remediation skill '{0}' is not registered, so its permissions cannot be checked and the finding cannot be handed over.";

    private const string LacksPermissionsReasonFormat =
        "Handing this finding over approves its remediation '{0}', which requires permissions you do not hold.";

    private const string SupersededByDelegationReason = "the finding was delegated and thereby approved directly";

    private const string ApprovalNotStampedMessage =
        "Delegation of condition {ConditionId} by {UserId} was stored, but the approval stamp was not written "
        + "(the row is no longer Reported or somebody approved it first)";

    private readonly IProactiveTriggerDispatchRepository _dispatchRepository;
    private readonly IAgentConditionScopeResolver _scopeResolver;
    private readonly IAgentConditionRepository _conditionRepository;
    private readonly IAgentConditionLedgerService _ledgerService;
    private readonly IConditionRemediationRegistry _remediationRegistry;
    private readonly ISkillRegistry _skillRegistry;
    private readonly ISkillPermissionGate _permissionGate;
    private readonly IEscalationChainService _chainService;
    private readonly ILogger<DelegateConditionCommandHandler> _logger;

    public DelegateConditionCommandHandler(
        IProactiveTriggerDispatchRepository dispatchRepository,
        IAgentConditionScopeResolver scopeResolver,
        IAgentConditionRepository conditionRepository,
        IAgentConditionLedgerService ledgerService,
        IConditionRemediationRegistry remediationRegistry,
        ISkillRegistry skillRegistry,
        ISkillPermissionGate permissionGate,
        IEscalationChainService chainService,
        ILogger<DelegateConditionCommandHandler> logger)
    {
        _dispatchRepository = dispatchRepository;
        _scopeResolver = scopeResolver;
        _conditionRepository = conditionRepository;
        _ledgerService = ledgerService;
        _remediationRegistry = remediationRegistry;
        _skillRegistry = skillRegistry;
        _permissionGate = permissionGate;
        _chainService = chainService;
        _logger = logger;
    }

    public async Task<DelegateConditionResult> Handle(DelegateConditionCommand request, CancellationToken cancellationToken)
    {
        var row = await _dispatchRepository.GetByIdAsync(request.MessageId, cancellationToken);
        if (row == null
            || !string.Equals(row.UserId, request.DelegatingUserId.ToString(), StringComparison.OrdinalIgnoreCase)
            || row.ConditionId is not Guid conditionId)
        {
            return DelegateConditionResult.NotFound;
        }

        var scope = await _scopeResolver.ResolveAsync(request.DelegatingUserId.ToString(), cancellationToken);
        if (!scope.IsPlanner)
        {
            return DelegateConditionResult.NotFound;
        }

        var condition = await _conditionRepository.GetOpenForScopeByIdAsync(
            conditionId, scope.IsUnrestricted, scope.VisibleRootIds, cancellationToken);
        if (condition == null)
        {
            return DelegateConditionResult.NotFound;
        }

        var refusal = await CheckRightsAsync(condition, request.DelegatingUserId, cancellationToken);
        if (refusal is not null)
        {
            return refusal;
        }

        var delegated = await _ledgerService.TryDelegateAsync(
            conditionId, request.MaxAction, request.DelegatingUserId, cancellationToken);

        if (!delegated)
        {
            return DelegateConditionResult.NotFound;
        }

        if (condition.Status == AgentConditionStatus.Reported)
        {
            await ApproveAsync(conditionId, request.DelegatingUserId, cancellationToken);
        }

        await AcknowledgeDispatchRowAsync(row.Id, row.UserId, cancellationToken);

        return DelegateConditionResult.Delegated;
    }

    private async Task<DelegateConditionResult?> CheckRightsAsync(
        AgentCondition condition, Guid delegatingUserId, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (!_remediationRegistry.TryGetEntry(condition.TriggerKind, out var entry) || entry is null)
        {
            return DelegateConditionResult.Forbidden(NoRemediationReason);
        }

        var descriptor = _skillRegistry.GetSkillByName(entry.RemediationSkillName);
        if (descriptor is null)
        {
            return DelegateConditionResult.Forbidden(
                string.Format(CultureInfo.InvariantCulture, UnknownSkillReasonFormat, entry.RemediationSkillName));
        }

        if (!await _permissionGate.HoldsAsync(delegatingUserId.ToString(), descriptor.RequiredPermissions))
        {
            return DelegateConditionResult.Forbidden(
                string.Format(CultureInfo.InvariantCulture, LacksPermissionsReasonFormat, entry.RemediationSkillName));
        }

        return null;
    }

    /// <summary>
    /// The chain is ended first so no further stage is woken, then the stamp is written. Both are
    /// compare-and-swaps, so a roster member acknowledging at the same moment ends with exactly one
    /// approver either way: whoever's stamp lands first is the approver, the other stamp is refused.
    /// </summary>
    private async Task ApproveAsync(Guid conditionId, Guid delegatingUserId, CancellationToken cancellationToken)
    {
        await _chainService.SupersedeConditionApprovalChainAsync(conditionId, SupersededByDelegationReason, cancellationToken);

        var stamped = await _ledgerService.TryApproveAsync(conditionId, delegatingUserId, cancellationToken);
        if (!stamped)
        {
            _logger.LogInformation(ApprovalNotStampedMessage, conditionId, delegatingUserId);
        }
    }

    /// <summary>
    /// Delegating is the user's "mach du" answer to the message, so it acknowledges the dispatch row and
    /// ends its reminder loop. Best-effort: the grant is already written and must not fail because the
    /// acknowledgement did.
    /// </summary>
    private async Task AcknowledgeDispatchRowAsync(Guid rowId, string userId, CancellationToken cancellationToken)
    {
        try
        {
            await _dispatchRepository.AcknowledgeAsync(rowId, userId, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Acknowledging dispatch row {RowId} after delegation failed; the delegation itself is stored", rowId);
        }
    }
}
