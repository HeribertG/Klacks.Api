// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Repository for ShiftRequiredQualification. GetActiveAsync returns the tracked active row for a
/// (shift, qualification) pair so the set-command handler can upsert it in place. GetEffectiveByShiftIdsAsync resolves
/// whose rows apply to a staffed shift (ShiftRequirementSourceResolver), because a cut piece created in the cut dialog
/// carries no requirement rows of its own.
/// </summary>

using Klacks.Api.Domain.Interfaces.Associations;
using Klacks.Api.Domain.Services.Schedules;
using Klacks.Api.Domain.Models.Associations;
using Klacks.Api.Infrastructure.Persistence;
using Klacks.Api.Infrastructure.Repositories.Schedules;
using Microsoft.EntityFrameworkCore;

namespace Klacks.Api.Infrastructure.Repositories.Associations;

public class ShiftRequiredQualificationRepository : BaseRepository<ShiftRequiredQualification>, IShiftRequiredQualificationRepository
{
    public ShiftRequiredQualificationRepository(DataBaseContext context, ILogger<ShiftRequiredQualification> logger)
        : base(context, logger)
    {
    }

    public async Task<ShiftRequiredQualification?> GetActiveAsync(
        Guid shiftId, Guid qualificationId, CancellationToken ct = default)
    {
        return await context.ShiftRequiredQualification
            .FirstOrDefaultAsync(srq => srq.ShiftId == shiftId && srq.QualificationId == qualificationId, ct);
    }

    public async Task<List<ShiftRequiredQualification>> GetByShiftIdAsync(
        Guid shiftId, CancellationToken ct = default)
    {
        return await context.ShiftRequiredQualification
            .Where(srq => srq.ShiftId == shiftId)
            .ToListAsync(ct);
    }

    public async Task<List<ShiftRequiredQualification>> GetByShiftIdsAsync(
        IReadOnlyCollection<Guid> shiftIds, CancellationToken ct = default)
    {
        var ids = shiftIds.ToList();
        return await context.ShiftRequiredQualification
            .Include(srq => srq.Qualification)
            .Include(srq => srq.Shift)
            .Where(srq => ids.Contains(srq.ShiftId))
            .ToListAsync(ct);
    }

    public async Task<List<EffectiveShiftRequirement>> GetEffectiveByShiftIdsAsync(
        IReadOnlyCollection<Guid> shiftIds, CancellationToken ct = default)
    {
        var ids = shiftIds.Distinct().ToList();
        if (ids.Count == 0)
        {
            return [];
        }

        var rows = await ShiftTreeQuery.LoadFamiliesAsync(context, ids, ct);
        var candidateIds = rows.Select(r => r.Id).Concat(ids).Distinct().ToList();
        var requirementsByShift = (await context.ShiftRequiredQualification
                .AsNoTracking()
                .Include(srq => srq.Qualification)
                .Where(srq => candidateIds.Contains(srq.ShiftId))
                .ToListAsync(ct))
            .ToLookup(srq => srq.ShiftId);
        var shiftsWithOwnRows = requirementsByShift.Select(g => g.Key).ToHashSet();

        var sources = ShiftRequirementSourceResolver.ResolveSources(ids, rows, shiftsWithOwnRows);
        if (sources.Count == 0)
        {
            return [];
        }

        var receiverIds = sources.Keys.ToList();
        var names = await context.Shift
            .AsNoTracking()
            .Where(s => receiverIds.Contains(s.Id))
            .Select(s => new { s.Id, s.Name, s.Abbreviation })
            .ToDictionaryAsync(s => s.Id, s => string.IsNullOrWhiteSpace(s.Name) ? s.Abbreviation : s.Name, ct);

        return sources
            .OrderBy(entry => entry.Key)
            .SelectMany(entry => requirementsByShift[entry.Value].Select(requirement => new EffectiveShiftRequirement(
                entry.Key,
                names.GetValueOrDefault(entry.Key, string.Empty),
                entry.Value,
                requirement)))
            .ToList();
    }
}