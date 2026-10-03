// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Creates a planning constraint on behalf of an admin. The server assigns the id and marks the row
/// Origin Admin / Approved with the admin as proposer and approver; import keys stay empty, so a later
/// region-setup re-import never touches it.
/// </summary>
/// <param name="request">Editable fields of the constraint and the acting admin's user id</param>

using Klacks.Api.Application.Commands.PlanningConstraints;
using Klacks.Api.Application.DTOs.Scheduling;
using Klacks.Api.Application.Interfaces.Schedules;
using Klacks.Api.Application.Mappers;
using Klacks.Api.Domain.Exceptions;
using Klacks.Api.Domain.Interfaces;
using Klacks.Api.Domain.Interfaces.Scheduling;
using Klacks.Api.Domain.Services.Schedules;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Handlers.PlanningConstraints;

public class CreatePlanningConstraintCommandHandler : BaseHandler, IRequestHandler<CreatePlanningConstraintCommand, PlanningConstraintResource>
{
    private readonly IPlanningConstraintRepository _repository;
    private readonly IPlanningConstraintValidator _validator;
    private readonly IPlanningConstraintReferenceReader _references;
    private readonly PlanningConstraintMapper _mapper;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IPlanningConstraintPresence _presence;
    private readonly TimeProvider _timeProvider;

    public CreatePlanningConstraintCommandHandler(
        IPlanningConstraintRepository repository,
        IPlanningConstraintValidator validator,
        IPlanningConstraintReferenceReader references,
        PlanningConstraintMapper mapper,
        IUnitOfWork unitOfWork,
        TimeProvider timeProvider,
        IPlanningConstraintPresence presence,
        ILogger<CreatePlanningConstraintCommandHandler> logger)
        : base(logger)
    {
        _repository = repository;
        _validator = validator;
        _references = references;
        _mapper = mapper;
        _unitOfWork = unitOfWork;
        _presence = presence;
        _timeProvider = timeProvider;
    }

    public async Task<PlanningConstraintResource> Handle(CreatePlanningConstraintCommand request, CancellationToken cancellationToken)
    {
        if (request.Resource is null)
        {
            throw new InvalidRequestException("Planning constraint data is required.");
        }

        var entity = _mapper.ToEntity(request.Resource);
        entity.Id = Guid.NewGuid();
        entity.ImportSourceKey = string.Empty;
        entity.ImportContentHash = string.Empty;
        PlanningConstraintLifecycle.MarkAdminApproved(entity, request.Actor, _timeProvider.GetUtcNow().UtcDateTime);
        PlanningConstraintGuard.EnsureValid(_validator, entity);
        await PlanningConstraintGuard.EnsureReferencesExistAsync(_references, entity, cancellationToken);

        return await ExecuteAsync(async () =>
        {
            _repository.Add(entity);
            await PlanningConstraintGuard.SaveAsync(_unitOfWork, _presence);
            return _mapper.ToResource(entity);
        },
        "creating planning constraint",
        new { entity.Kind, entity.ScopeType });
    }
}
