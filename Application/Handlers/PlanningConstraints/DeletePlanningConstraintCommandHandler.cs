// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Soft-deletes a Proposed or Rejected planning constraint. Approved rows are revoked instead and Revoked rows
/// stay as audit trail and as links of the PreviousVersionId chain (both 409), so a rule that was ever
/// effective can never disappear from the history.
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
        if (constraint.ApprovalStatus is not (RuleApprovalStatus.Proposed or RuleApprovalStatus.Rejected))
        {
            throw new ConflictException(constraint.ApprovalStatus == RuleApprovalStatus.Approved
                ? $"Planning constraint {request.Id} is Approved; revoke it instead of deleting it."
                : $"Planning constraint {request.Id} is Revoked and stays as audit trail of its version chain.");
        }

        return await ExecuteAsync(async () =>
        {
            _repository.Remove(constraint);
            await PlanningConstraintGuard.SaveAsync(_unitOfWork);
            return _mapper.ToResource(constraint);
        },
        "deleting planning constraint",
        new { request.Id });
    }
}
