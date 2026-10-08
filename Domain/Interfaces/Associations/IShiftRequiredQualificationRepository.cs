// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

using Klacks.Api.Domain.Models.Associations;

namespace Klacks.Api.Domain.Interfaces.Associations;

public interface IShiftRequiredQualificationRepository : IBaseRepository<ShiftRequiredQualification>
{
    Task<ShiftRequiredQualification?> GetActiveAsync(Guid shiftId, Guid qualificationId, CancellationToken ct = default);
    Task<List<ShiftRequiredQualification>> GetByShiftIdAsync(Guid shiftId, CancellationToken ct = default);
    Task<List<ShiftRequiredQualification>> GetByShiftIdsAsync(IReadOnlyCollection<Guid> shiftIds, CancellationToken ct = default);

    /// <summary>
    /// Requirements each shift inherits from its order and its cut ancestors (ShiftScopeExpander), expressed as detached,
    /// never persisted rows of the receiving shift: ShiftId = receiver, Shift = name-only stub of the receiver. Own rows of
    /// the receiver are not included (see GetByShiftIdsAsync).
    /// </summary>
    Task<List<ShiftRequiredQualification>> GetInheritedByShiftIdsAsync(IReadOnlyCollection<Guid> shiftIds, CancellationToken ct = default);
}
