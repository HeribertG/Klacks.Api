// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

using Klacks.Api.Domain.Models.Associations;

namespace Klacks.Api.Domain.Interfaces.Associations;

public interface IShiftRequiredQualificationRepository : IBaseRepository<ShiftRequiredQualification>
{
    Task<ShiftRequiredQualification?> GetActiveAsync(Guid shiftId, Guid qualificationId, CancellationToken ct = default);
    Task<List<ShiftRequiredQualification>> GetByShiftIdAsync(Guid shiftId, CancellationToken ct = default);
    Task<List<ShiftRequiredQualification>> GetByShiftIdsAsync(IReadOnlyCollection<Guid> shiftIds, CancellationToken ct = default);

    /// <summary>
    /// The requirement rows that apply to each staffed shift, the single place of the rule in
    /// <see cref="Klacks.Api.Domain.Services.Schedules.ShiftRequirementSourceResolver"/>: the nearest link of shift ->
    /// cut ancestors -> plannable copy -> sealed order with rows of its own wins, no merge. Optional rows included;
    /// a shift with no applicable row is absent. Rows are untracked and keep the ShiftId of the shift that carries them.
    /// </summary>
    Task<List<EffectiveShiftRequirement>> GetEffectiveByShiftIdsAsync(IReadOnlyCollection<Guid> shiftIds, CancellationToken ct = default);
}