// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Application.Interfaces;
using Klacks.Api.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Klacks.Api.Infrastructure.Services;

public class FloorPlanMarkerDataLookup : IFloorPlanMarkerDataLookup
{
    private readonly DataBaseContext _context;

    public FloorPlanMarkerDataLookup(DataBaseContext context)
    {
        _context = context;
    }

    public async Task<Dictionary<Guid, string>> GetClientNamesAsync(List<Guid> clientIds)
    {
        if (!clientIds.Any())
        {
            return new Dictionary<Guid, string>();
        }

        return await _context.Client
            .Where(c => clientIds.Contains(c.Id) && !c.IsDeleted)
            .ToDictionaryAsync(
                c => c.Id,
                c => $"{c.FirstName} {c.Name}".Trim());
    }

    public async Task<Dictionary<Guid, FloorPlanShiftDetail>> GetShiftDetailsAsync(List<Guid> shiftIds)
    {
        if (!shiftIds.Any())
        {
            return new Dictionary<Guid, FloorPlanShiftDetail>();
        }

        return await _context.Shift
            .Where(s => shiftIds.Contains(s.Id) && !s.IsDeleted)
            .ToDictionaryAsync(
                s => s.Id,
                s => new FloorPlanShiftDetail(
                    s.Abbreviation,
                    s.StartShift,
                    s.EndShift));
    }
}
