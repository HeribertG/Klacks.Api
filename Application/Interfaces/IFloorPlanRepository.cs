// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Domain.Interfaces;
using Klacks.Api.Domain.Models.FloorPlans;

namespace Klacks.Api.Application.Interfaces;

public interface IFloorPlanRepository : IBaseRepository<FloorPlan>
{
    Task<List<FloorPlan>> GetAllWithMarkersAsync();

    Task<FloorPlan?> GetWithMarkersAsync(Guid id);
}
