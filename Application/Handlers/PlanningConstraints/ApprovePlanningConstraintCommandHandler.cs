// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Approves a Proposed planning constraint from the UI pending list. The row is validated again (a proposal
/// may have been stored by an older schema) and must not have expired - a proposal older than the lifetime is
/// refused even if the expiry sweep has not reached it yet. Only reachable through the Admin REST endpoint.
/// </summary>
/// <param name="request">Constraint id and the approving admin's user id</param>

using Klacks.Api.Application.Commands.PlanningConstraints;
using Klacks.Api.Application.DTOs.Scheduling;
using Klacks.Api.Application.Exceptions;
using Klacks.Api.Application.Mappers;
using Klacks.Api.Domain.Interfaces;
using Klacks.Api.Domain.Interfaces.Scheduling;
using Klacks.Api.Domain.Services.Schedules;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Handlers.PlanningConstraints;

public class ApprovePlanningConstraintCommandHandler : BaseHandler, IRequestHandler<ApprovePlanningConstraintCommand, PlanningConstraintResource>
{
    private readonly IPlanningConstraintRepository _repository;
    private readonly IPlanningConstraintValidator _validator;
    private readonly IPlanningConstraintReferenceReader _references;
    private readonly PlanningConstraintMapper _mapper;
    private readonly IUnitOfWork _unitOfWork;
    private readonly TimeProvider _timeProvider;

    public ApprovePlanningConstraintCommandHandler(
        IPlanningConstraintRepository repository,
        IPlanningConstraintValidator validator,
        IPlanningConstraintReferenceReader references,
        PlanningConstraintMapper mapper,
        IUnitOfWork unitOfWork,
        TimeProvider timeProvider,
        ILogger<ApprovePlanningConstraintCommandHandler> logger)
        : base(logger)
    {
        _repository = repository;
        _validator = validator;
        _references = references;
        _mapper = mapper;
        _unitOfWork = unitOfWork;
        _timeProvider = timeProvider;
    }

    public async Task<PlanningConstraintResource> Handle(ApprovePlanningConstraintCommand request, CancellationToken cancellationToken)
    {
        var constraint = await PlanningConstraintGuard.GetExistingAsync(_repository, request.Id, cancellationToken);
        PlanningConstraintGuard.EnsureValid(_validator, constraint);
        await PlanningConstraintGuard.EnsureReferencesExistAsync(_references, constraint, cancellationToken);

        var nowUtc = _timeProvider.GetUtcNow().UtcDateTime;
        if (!PlanningConstraintLifecycle.TryApprove(constraint, request.Actor, nowUtc))
        {
            throw new ConflictException(PlanningConstraintLifecycle.IsProposalExpired(constraint, nowUtc)
                ? $"Planning constraint {request.Id} has expired and can no longer be approved."
                : $"Planning constraint {request.Id} is {constraint.ApprovalStatus}; only a Proposed constraint can be approved.");
        }

        return await ExecuteAsync(async () =>
        {
            await PlanningConstraintGuard.SaveAsync(_unitOfWork);
            return _mapper.ToResource(constraint);
        },
        "approving planning constraint",
        new { request.Id });
    }
}
