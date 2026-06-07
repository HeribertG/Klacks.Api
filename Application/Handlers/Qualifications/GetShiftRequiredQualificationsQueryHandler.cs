// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Handler for <see cref="GetShiftRequiredQualificationsQuery"/>. Returns all active required-qualification
/// rows for the requested shift, projected to the resource DTO.
/// </summary>
/// <param name="repository">Reads the shift-required-qualification rows</param>

using Klacks.Api.Application.DTOs.Associations;
using Klacks.Api.Application.Queries.Qualifications;
using Klacks.Api.Domain.Interfaces.Associations;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Handlers.Qualifications;

public sealed class GetShiftRequiredQualificationsQueryHandler
    : IRequestHandler<GetShiftRequiredQualificationsQuery, List<ShiftRequiredQualificationResource>>
{
    private readonly IShiftRequiredQualificationRepository _repository;

    public GetShiftRequiredQualificationsQueryHandler(IShiftRequiredQualificationRepository repository)
    {
        _repository = repository;
    }

    public async Task<List<ShiftRequiredQualificationResource>> Handle(
        GetShiftRequiredQualificationsQuery request, CancellationToken cancellationToken)
    {
        var list = await _repository.GetByShiftIdAsync(request.ShiftId, cancellationToken);
        return list.Select(srq => new ShiftRequiredQualificationResource
        {
            Id = srq.Id,
            ShiftId = srq.ShiftId,
            QualificationId = srq.QualificationId,
            IsMandatory = srq.IsMandatory,
            MinLevel = srq.MinLevel,
        }).ToList();
    }
}
