// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Application.Interfaces;
using Klacks.Api.Domain.Models.FloorPlans;
using Klacks.Api.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Klacks.Api.Infrastructure.Repositories.FloorPlans;

public class FloorPlanRepository : BaseRepository<FloorPlan>, IFloorPlanRepository
{
    private readonly DataBaseContext _context;

    public FloorPlanRepository(
        DataBaseContext context,
        ILogger<FloorPlan> logger)
        : base(context, logger)
    {
        _context = context;
    }

    public async Task<List<FloorPlan>> GetAllWithMarkersAsync()
    {
        return await _context.FloorPlan
            .Include(fp => fp.WorkMarkers)
            .Where(fp => !fp.IsDeleted)
            .ToListAsync();
    }

    public async Task<FloorPlan?> GetWithMarkersAsync(Guid id)
    {
        return await _context.FloorPlan
            .Include(fp => fp.WorkMarkers)
            .FirstOrDefaultAsync(fp => fp.Id == id && !fp.IsDeleted);
    }
}
