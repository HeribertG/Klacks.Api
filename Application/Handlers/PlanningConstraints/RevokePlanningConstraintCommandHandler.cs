// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Revokes an Approved planning constraint; the row stays for the audit trail and is no longer evaluated.
/// Any other state answers 409.
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

public class RevokePlanningConstraintCommandHandler : BaseHandler, IRequestHandler<RevokePlanningConstraintCommand, PlanningConstraintResource>
{
    private readonly IPlanningConstraintRepository _repository;
    private readonly PlanningConstraintMapper _mapper;
    private readonly IUnitOfWork _unitOfWork;

    public RevokePlanningConstraintCommandHandler(
        IPlanningConstraintRepository repository,
        PlanningConstraintMapper mapper,
        IUnitOfWork unitOfWork,
        ILogger<RevokePlanningConstraintCommandHandler> logger)
        : base(logger)
    {
        _repository = repository;
        _mapper = mapper;
        _unitOfWork = unitOfWork;
    }

    public async Task<PlanningConstraintResource> Handle(RevokePlanningConstraintCommand request, CancellationToken cancellationToken)
    {
        var constraint = await PlanningConstraintGuard.GetExistingAsync(_repository, request.Id, cancellationToken);
        if (!PlanningConstraintLifecycle.TryRevoke(constraint))
        {
            throw new ConflictException($"Planning constraint {request.Id} is {constraint.ApprovalStatus}; only an Approved constraint can be revoked.");
        }

        return await ExecuteAsync(async () =>
        {
            await PlanningConstraintGuard.SaveAsync(_unitOfWork);
            return _mapper.ToResource(constraint);
        },
        "revoking planning constraint",
        new { request.Id });
    }
}
