// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Changes a planning constraint. Proposed rows are edited in place and stay Proposed. Approved rows are
/// immutable: the change is stored as a new Admin/Approved row pointing back via PreviousVersionId (same
/// scenario token), and the old row is set to Revoked in the same save, so exactly one version is ever
/// effective. Rejected and Revoked rows are final (409).
/// </summary>
/// <param name="request">Target id, new editable fields and the acting admin's user id</param>

using Klacks.Api.Application.Commands.PlanningConstraints;
using Klacks.Api.Application.DTOs.Scheduling;
using Klacks.Api.Application.Exceptions;
using Klacks.Api.Application.Mappers;
using Klacks.Api.Domain.Enums;
using Klacks.Api.Domain.Exceptions;
using Klacks.Api.Domain.Interfaces;
using Klacks.Api.Domain.Interfaces.Scheduling;
using Klacks.Api.Domain.Models.Scheduling;
using Klacks.Api.Domain.Services.Schedules;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Handlers.PlanningConstraints;

public class UpdatePlanningConstraintCommandHandler : BaseHandler, IRequestHandler<UpdatePlanningConstraintCommand, PlanningConstraintResource>
{
    private readonly IPlanningConstraintRepository _repository;
    private readonly IPlanningConstraintValidator _validator;
    private readonly IPlanningConstraintReferenceReader _references;
    private readonly PlanningConstraintMapper _mapper;
    private readonly IUnitOfWork _unitOfWork;
    private readonly TimeProvider _timeProvider;

    public UpdatePlanningConstraintCommandHandler(
        IPlanningConstraintRepository repository,
        IPlanningConstraintValidator validator,
        IPlanningConstraintReferenceReader references,
        PlanningConstraintMapper mapper,
        IUnitOfWork unitOfWork,
        TimeProvider timeProvider,
        ILogger<UpdatePlanningConstraintCommandHandler> logger)
        : base(logger)
    {
        _repository = repository;
        _validator = validator;
        _references = references;
        _mapper = mapper;
        _unitOfWork = unitOfWork;
        _timeProvider = timeProvider;
    }

    public async Task<PlanningConstraintResource> Handle(UpdatePlanningConstraintCommand request, CancellationToken cancellationToken)
    {
        if (request.Resource is null)
        {
            throw new InvalidRequestException("Planning constraint data is required.");
        }

        var existing = await PlanningConstraintGuard.GetExistingAsync(_repository, request.Id, cancellationToken);
        var result = existing.ApprovalStatus switch
        {
            RuleApprovalStatus.Proposed => EditInPlace(existing, request.Resource),
            RuleApprovalStatus.Approved => Supersede(existing, request.Resource, request.Actor),
            _ => throw new ConflictException($"Planning constraint {request.Id} is {existing.ApprovalStatus} and can no longer be changed."),
        };
        await PlanningConstraintGuard.EnsureReferencesExistAsync(_references, result, cancellationToken);

        return await ExecuteAsync(async () =>
        {
            await PlanningConstraintGuard.SaveAsync(_unitOfWork);
            return _mapper.ToResource(result);
        },
        "updating planning constraint",
        new { request.Id, existing.ApprovalStatus });
    }

    private PlanningConstraint EditInPlace(PlanningConstraint existing, PlanningConstraintWriteResource resource)
    {
        _mapper.ApplyEditableFields(resource, existing);
        PlanningConstraintGuard.EnsureValid(_validator, existing);
        return existing;
    }

    private PlanningConstraint Supersede(PlanningConstraint existing, PlanningConstraintWriteResource resource, string actor)
    {
        var successor = _mapper.ToEntity(resource);
        successor.Id = Guid.NewGuid();
        successor.AnalyseToken = existing.AnalyseToken;
        successor.PreviousVersionId = existing.Id;
        successor.ImportSourceKey = string.Empty;
        successor.ImportContentHash = string.Empty;
        PlanningConstraintLifecycle.MarkAdminApproved(successor, actor, _timeProvider.GetUtcNow().UtcDateTime);
        PlanningConstraintGuard.EnsureValid(_validator, successor);

        PlanningConstraintLifecycle.TryRevoke(existing);
        _repository.Add(successor);
        return successor;
    }
}
