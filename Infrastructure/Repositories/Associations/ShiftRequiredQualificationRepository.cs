// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Repository for ShiftRequiredQualification. GetActiveAsync returns the tracked active row for a
/// (shift, qualification) pair so the set-command handler can upsert it in place.
/// </summary>

using Klacks.Api.Domain.Interfaces.Associations;
using Klacks.Api.Domain.Models.Associations;
using Klacks.Api.Infrastructure.Persistence;
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
}
