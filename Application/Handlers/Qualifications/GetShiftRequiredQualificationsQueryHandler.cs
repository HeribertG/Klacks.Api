// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Handler for <see cref="GetShiftRequiredQualificationsQuery"/>. Returns all active required-qualification
/// rows for the requested shift, projected to the resource DTO. A shift a group-restricted caller may not see yields an
/// empty list, exactly like a missing shift.
/// </summary>
/// <param name="repository">Reads the shift-required-qualification rows</param>
/// <param name="shiftVisibilityGuard">Decides whether the caller may see the shift</param>

using Klacks.Api.Application.DTOs.Associations;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Application.Queries.Qualifications;
using Klacks.Api.Domain.Interfaces.Associations;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Handlers.Qualifications;

public sealed class GetShiftRequiredQualificationsQueryHandler
    : IRequestHandler<GetShiftRequiredQualificationsQuery, List<ShiftRequiredQualificationResource>>
{
    private readonly IShiftRequiredQualificationRepository _repository;
    private readonly IShiftVisibilityGuard _shiftVisibilityGuard;

    public GetShiftRequiredQualificationsQueryHandler(
        IShiftRequiredQualificationRepository repository, IShiftVisibilityGuard shiftVisibilityGuard)
    {
        _repository = repository;
        _shiftVisibilityGuard = shiftVisibilityGuard;
    }

    public async Task<List<ShiftRequiredQualificationResource>> Handle(
        GetShiftRequiredQualificationsQuery request, CancellationToken cancellationToken)
    {
        if (!await _shiftVisibilityGuard.IsVisibleAsync(request.ShiftId, cancellationToken))
        {
            return [];
        }

        var list = await _repository.GetByShiftIdAsync(request.ShiftId, cancellationToken);
        return list.Select(srq => new ShiftRequiredQualificationResource
        {
            Id = srq.Id,
            ShiftId = srq.ShiftId,
            QualificationId = srq.QualificationId,
            IsMandatory = srq.IsMandatory,
            MinLevel = srq.MinLevel,
            LevelBindingOverride = srq.LevelBindingOverride,
        }).ToList();
    }
}
