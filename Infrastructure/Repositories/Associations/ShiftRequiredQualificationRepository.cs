// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Repository for ShiftRequiredQualification. GetActiveAsync returns the tracked active row for a
/// (shift, qualification) pair so the set-command handler can upsert it in place. GetInheritedByShiftIdsAsync hands
/// the requirements of an order and of cut ancestors down to the cut pieces (ShiftScopeExpander), because a cut piece
/// created in the cut dialog carries no requirement rows of its own.
/// </summary>

using Klacks.Api.Domain.Interfaces.Associations;
using Klacks.Api.Domain.Models.Schedules;
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

    public async Task<List<ShiftRequiredQualification>> GetInheritedByShiftIdsAsync(
        IReadOnlyCollection<Guid> shiftIds, CancellationToken ct = default)
    {
        var rows = await ShiftTreeQuery.LoadFamiliesAsync(context, shiftIds, ct);
        var sourcesByReceiver = ShiftScopeExpander.InheritanceSourcesOf(shiftIds, rows);
        if (sourcesByReceiver.Count == 0)
        {
            return [];
        }

        var sourceIds = sourcesByReceiver.Values.SelectMany(s => s).Distinct().ToList();
        var sourceRequirements = await context.ShiftRequiredQualification
            .AsNoTracking()
            .Include(srq => srq.Qualification)
            .Where(srq => sourceIds.Contains(srq.ShiftId))
            .ToListAsync(ct);
        if (sourceRequirements.Count == 0)
        {
            return [];
        }

        var receiverIds = sourcesByReceiver.Keys.ToList();
        var receivers = await context.Shift
            .AsNoTracking()
            .Where(s => receiverIds.Contains(s.Id))
            .Select(s => new { s.Id, s.Name, s.Abbreviation })
            .ToDictionaryAsync(s => s.Id, ct);

        var requirementsBySource = sourceRequirements.ToLookup(srq => srq.ShiftId);
        var inherited = new List<ShiftRequiredQualification>();
        foreach (var (receiverId, sources) in sourcesByReceiver)
        {
            var receiverShift = receivers.TryGetValue(receiverId, out var receiver)
                ? new Shift { Id = receiver.Id, Name = receiver.Name, Abbreviation = receiver.Abbreviation }
                : null;
            foreach (var source in sources.OrderBy(id => id))
            {
                inherited.AddRange(requirementsBySource[source].Select(requirement => new ShiftRequiredQualification
                {
                    Id = requirement.Id,
                    ShiftId = receiverId,
                    QualificationId = requirement.QualificationId,
                    IsMandatory = requirement.IsMandatory,
                    MinLevel = requirement.MinLevel,
                    Qualification = requirement.Qualification,
                    Shift = receiverShift,
                }));
            }
        }

        return inherited;
    }
}