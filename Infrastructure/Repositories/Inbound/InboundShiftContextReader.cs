// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Read-only projection of a client's planned shifts for the clarification question: real plan rows
/// only (not soft-deleted, no analyse-scenario rows, no container sub-rows via ParentWorkId, and
/// rows whose Shift is soft-deleted are excluded by the required-navigation query filter), ordered
/// by date and start time, with the shift name as stored. A dedicated narrow query because
/// IWorkRepository.GetByClientAndDateRangeAsync neither filters scenario rows nor loads the shift.
/// </summary>
/// <param name="context">The scoped database context</param>

using Klacks.Api.Domain.Interfaces.Inbound;
using Klacks.Api.Domain.Models.Inbound;
using Klacks.Api.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Klacks.Api.Infrastructure.Repositories.Inbound;

public class InboundShiftContextReader : IInboundShiftContextReader
{
    private readonly DataBaseContext _context;

    public InboundShiftContextReader(DataBaseContext context)
    {
        _context = context;
    }

    public async Task<IReadOnlyList<ClarificationShift>> GetShiftsAsync(
        Guid clientId, DateOnly fromDate, DateOnly untilDate, int maxCount, CancellationToken cancellationToken = default)
    {
        return await _context.Work
            .AsNoTracking()
            .Where(w => w.ClientId == clientId
                        && !w.IsDeleted
                        && w.ParentWorkId == null
                        && w.AnalyseToken == null
                        && w.CurrentDate >= fromDate
                        && w.CurrentDate <= untilDate)
            .OrderBy(w => w.CurrentDate)
            .ThenBy(w => w.StartTime)
            .Take(maxCount)
            .Select(w => new ClarificationShift(
                w.CurrentDate,
                w.StartTime,
                w.EndTime,
                w.Shift != null ? w.Shift.Name : string.Empty))
            .ToListAsync(cancellationToken);
    }
}
