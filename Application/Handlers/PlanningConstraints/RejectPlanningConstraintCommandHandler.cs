// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Rejects a Proposed planning constraint from the UI pending list; any other state answers 409.
/// </summary>
/// <param name="request">Constraint id</param>

using Klacks.Api.Application.Commands.PlanningConstraints;
using Klacks.Api.Application.DTOs.Scheduling;
using Klacks.Api.Application.Exceptions;
using Klacks.Api.Application.Mappers;
using Klacks.Api.Domain.Interfaces;
using Klacks.Api.Domain.Interfaces.Scheduling;
using Klacks.Api.Domain.Services.Schedules;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Handlers.PlanningConstraints;

public class RejectPlanningConstraintCommandHandler : BaseHandler, IRequestHandler<RejectPlanningConstraintCommand, PlanningConstraintResource>
{
    private readonly IPlanningConstraintRepository _repository;
    private readonly PlanningConstraintMapper _mapper;
    private readonly IUnitOfWork _unitOfWork;

    public RejectPlanningConstraintCommandHandler(
        IPlanningConstraintRepository repository,
        PlanningConstraintMapper mapper,
        IUnitOfWork unitOfWork,
        ILogger<RejectPlanningConstraintCommandHandler> logger)
        : base(logger)
    {
        _repository = repository;
        _mapper = mapper;
        _unitOfWork = unitOfWork;
    }

    public async Task<PlanningConstraintResource> Handle(RejectPlanningConstraintCommand request, CancellationToken cancellationToken)
    {
        var constraint = await PlanningConstraintGuard.GetExistingAsync(_repository, request.Id, cancellationToken);
        if (!PlanningConstraintLifecycle.TryReject(constraint))
        {
            throw new ConflictException($"Planning constraint {request.Id} is {constraint.ApprovalStatus}; only a Proposed constraint can be rejected.");
        }

        return await ExecuteAsync(async () =>
        {
            await PlanningConstraintGuard.SaveAsync(_unitOfWork);
            return _mapper.ToResource(constraint);
        },
        "rejecting planning constraint",
        new { request.Id });
    }
}
