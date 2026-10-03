// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Soft-deletes a planning constraint that is not Approved. An Approved row is effective and must be revoked
/// first (409), so deleting can never silently drop a rule without the audit trail of a revocation.
/// </summary>
/// <param name="request">Constraint id</param>

using Klacks.Api.Application.Commands.PlanningConstraints;
using Klacks.Api.Application.DTOs.Scheduling;
using Klacks.Api.Application.Exceptions;
using Klacks.Api.Application.Mappers;
using Klacks.Api.Domain.Enums;
using Klacks.Api.Domain.Interfaces;
using Klacks.Api.Domain.Interfaces.Scheduling;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Handlers.PlanningConstraints;

public class DeletePlanningConstraintCommandHandler : BaseHandler, IRequestHandler<DeletePlanningConstraintCommand, PlanningConstraintResource>
{
    private readonly IPlanningConstraintRepository _repository;
    private readonly PlanningConstraintMapper _mapper;
    private readonly IUnitOfWork _unitOfWork;

    public DeletePlanningConstraintCommandHandler(
        IPlanningConstraintRepository repository,
        PlanningConstraintMapper mapper,
        IUnitOfWork unitOfWork,
        ILogger<DeletePlanningConstraintCommandHandler> logger)
        : base(logger)
    {
        _repository = repository;
        _mapper = mapper;
        _unitOfWork = unitOfWork;
    }

    public async Task<PlanningConstraintResource> Handle(DeletePlanningConstraintCommand request, CancellationToken cancellationToken)
    {
        var constraint = await PlanningConstraintGuard.GetExistingAsync(_repository, request.Id, cancellationToken);
        if (constraint.ApprovalStatus == RuleApprovalStatus.Approved)
        {
            throw new ConflictException($"Planning constraint {request.Id} is Approved; revoke it before deleting it.");
        }

        return await ExecuteAsync(async () =>
        {
            _repository.Remove(constraint);
            await _unitOfWork.CompleteAsync();
            return _mapper.ToResource(constraint);
        },
        "deleting planning constraint",
        new { request.Id });
    }
}
