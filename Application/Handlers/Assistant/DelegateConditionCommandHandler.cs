// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Grants a single-condition "mach du" delegation (Etappe 4e). Two independent gates run before the
/// ledger is touched: a visibility gate (is the delegating user a planner at all, and is this specific
/// condition within their own group scope - answered NotFound either way, so an out-of-scope condition
/// is never revealed to exist) and a rights gate (does their role tier cover the requested MaxAction -
/// answered Forbidden, because by that point the condition is already known to be theirs to see). Both
/// reuse IAgentConditionScopeResolver, the same cached Admin/Authorised + GroupVisibility resolution
/// PlanningAudienceResolver and the Etappe 3g context block already rely on, rather than a second,
/// independent role check that could drift from it.
///
/// This only gates the GRANT. Once Etappe 5 acts on a delegation, the remediation itself still runs
/// under governance.ResponsibleOwnerUserId's current roles (Etappe 4d identity), not the delegating
/// user's - so a Prepare grant from an Authorised planner does not hand them Admin-level execution, it
/// only lets this one condition reach Prepare earlier than the kind's own governance would.
/// </summary>
/// <param name="dispatchRepository">Resolves the message id to the dispatch row it targets, and to the condition it reported.</param>
/// <param name="scopeResolver">Answers whether the delegating user may even see this condition.</param>
/// <param name="conditionRepository">Scoped existence check for the condition (Etappe 4e never trusts an unscoped id lookup).</param>
/// <param name="ledgerService">Writes the grant onto the condition-ledger row.</param>

using Klacks.Api.Application.Commands.Assistant;
using Klacks.Api.Domain.Enums;
using Klacks.Api.Domain.Interfaces.Assistant;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Handlers.Assistant;

public class DelegateConditionCommandHandler : IRequestHandler<DelegateConditionCommand, DelegateConditionOutcome>
{
    private readonly IProactiveTriggerDispatchRepository _dispatchRepository;
    private readonly IAgentConditionScopeResolver _scopeResolver;
    private readonly IAgentConditionRepository _conditionRepository;
    private readonly IAgentConditionLedgerService _ledgerService;

    public DelegateConditionCommandHandler(
        IProactiveTriggerDispatchRepository dispatchRepository,
        IAgentConditionScopeResolver scopeResolver,
        IAgentConditionRepository conditionRepository,
        IAgentConditionLedgerService ledgerService)
    {
        _dispatchRepository = dispatchRepository;
        _scopeResolver = scopeResolver;
        _conditionRepository = conditionRepository;
        _ledgerService = ledgerService;
    }

    public async Task<DelegateConditionOutcome> Handle(DelegateConditionCommand request, CancellationToken cancellationToken)
    {
        var row = await _dispatchRepository.GetByIdAsync(request.MessageId, cancellationToken);
        if (row == null
            || !string.Equals(row.UserId, request.DelegatingUserId.ToString(), StringComparison.OrdinalIgnoreCase)
            || row.ConditionId is not Guid conditionId)
        {
            return DelegateConditionOutcome.NotFound;
        }

        var scope = await _scopeResolver.ResolveAsync(request.DelegatingUserId.ToString(), cancellationToken);
        if (!scope.IsPlanner)
        {
            return DelegateConditionOutcome.NotFound;
        }

        var condition = await _conditionRepository.GetOpenForScopeByIdAsync(
            conditionId, scope.IsUnrestricted, scope.VisibleRootIds, cancellationToken);
        if (condition == null)
        {
            return DelegateConditionOutcome.NotFound;
        }

        if (!IsAllowedToDelegate(scope.IsUnrestricted, request.MaxAction))
        {
            return DelegateConditionOutcome.Forbidden;
        }

        var delegated = await _ledgerService.TryDelegateAsync(
            conditionId, request.MaxAction, request.DelegatingUserId, cancellationToken);

        return delegated ? DelegateConditionOutcome.Delegated : DelegateConditionOutcome.NotFound;
    }

    /// <summary>
    /// Admin (IsUnrestricted, per AgentConditionScopeResolver) may delegate up to Execute - the same
    /// ceiling they could already reach through agent_trigger_governance itself, which only Admins may
    /// write (set_proactive_governance is Sensitive). An Authorised planner may delegate only up to
    /// Prepare: they have no governance-writing rights at all, so Prepare is the most a single-condition
    /// grant can hand them without exceeding what their own role permits - a conservative reading where
    /// the plan left the exact ceiling unspecified, documented here rather than assumed silently.
    /// </summary>
    private static bool IsAllowedToDelegate(bool isUnrestricted, ProactiveMaxAction maxAction) =>
        isUnrestricted || maxAction <= ProactiveMaxAction.Prepare;
}
