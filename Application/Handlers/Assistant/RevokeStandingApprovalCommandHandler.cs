// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Withdraws a standing approval. The row is kept and stamped rather than deleted - it is the record of
/// an autonomy window that was open, and the ledger's executions under it point back to it.
///
/// What this does NOT stop is a finding whose claim was already taken under the grant: the approval stamp
/// then sits on the condition row itself, and the dispatcher resumes that one row from the stamp without
/// consulting the grant again. Such a row is bounded by AgentConditionActionDefaults.MaxAttemptsBeforeEscalation
/// and still re-checks the granter's live rights on every attempt, but a revocation is not retroactive.
/// </summary>
/// <param name="repository">Stamps the revocation; stage-only, this handler commits.</param>
/// <param name="unitOfWork">Commits the staged row.</param>
/// <param name="timeProvider">Clock the revocation is stamped from.</param>

using Klacks.Api.Application.Commands.Assistant;
using Klacks.Api.Domain.Interfaces;
using Klacks.Api.Domain.Interfaces.Assistant;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Handlers.Assistant;

public class RevokeStandingApprovalCommandHandler : IRequestHandler<RevokeStandingApprovalCommand, bool>
{
    private readonly IStandingApprovalRepository _repository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly TimeProvider _timeProvider;

    public RevokeStandingApprovalCommandHandler(
        IStandingApprovalRepository repository,
        IUnitOfWork unitOfWork,
        TimeProvider timeProvider)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
        _timeProvider = timeProvider;
    }

    public async Task<bool> Handle(RevokeStandingApprovalCommand request, CancellationToken cancellationToken)
    {
        var revoked = await _repository.TryRevokeAsync(
            request.Id,
            request.RevokedByUserId,
            _timeProvider.GetUtcNow().UtcDateTime,
            cancellationToken);

        if (!revoked)
        {
            return false;
        }

        await _unitOfWork.CompleteAsync();
        return true;
    }
}
