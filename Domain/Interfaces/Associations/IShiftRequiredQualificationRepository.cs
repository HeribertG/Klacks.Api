// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Domain.Models.Associations;

namespace Klacks.Api.Domain.Interfaces.Associations;

public interface IShiftRequiredQualificationRepository : IBaseRepository<ShiftRequiredQualification>
{
    Task<ShiftRequiredQualification?> GetActiveAsync(Guid shiftId, Guid qualificationId, CancellationToken ct = default);
}
