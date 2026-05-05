// Copyright (c) Heribert Gasparoli Private. All rights reserved.

namespace Klacks.Api.Application.Interfaces;

public interface IFloorPlanMarkerDataLookup
{
    Task<Dictionary<Guid, string>> GetClientNamesAsync(List<Guid> clientIds);

    Task<Dictionary<Guid, FloorPlanWorkShiftDetail>> GetWorkShiftDetailsAsync(List<Guid> workIds);
}

public record FloorPlanWorkShiftDetail(string? ShiftName, TimeOnly StartTime, TimeOnly EndTime);
