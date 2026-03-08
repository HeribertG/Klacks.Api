// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Application.Interfaces;
using Klacks.Api.Domain.Models.FloorPlans;
using Klacks.Api.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Klacks.Api.Infrastructure.Repositories.FloorPlans;

public class FloorPlanWorkMarkerRepository : BaseRepository<FloorPlanWorkMarker>, IFloorPlanWorkMarkerRepository
{
    private readonly DataBaseContext _context;

    public FloorPlanWorkMarkerRepository(
        DataBaseContext context,
        ILogger<FloorPlanWorkMarker> logger)
        : base(context, logger)
    {
        _context = context;
    }

    public async Task<List<FloorPlanWorkMarker>> GetByFloorPlanIdAsync(Guid floorPlanId)
    {
        return await _context.FloorPlanWorkMarker
            .Where(m => m.FloorPlanId == floorPlanId && !m.IsDeleted)
            .ToListAsync();
    }
}
