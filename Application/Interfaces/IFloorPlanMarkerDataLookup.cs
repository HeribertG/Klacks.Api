// Copyright (c) Heribert Gasparoli Private. All rights reserved.

namespace Klacks.Api.Application.Interfaces;

public interface IFloorPlanMarkerDataLookup
{
    Task<Dictionary<Guid, string>> GetClientNamesAsync(List<Guid> clientIds);

    Task<Dictionary<Guid, FloorPlanShiftDetail>> GetShiftDetailsAsync(List<Guid> shiftIds);
}

public record FloorPlanShiftDetail(string? Abbreviation, TimeOnly StartShift, TimeOnly EndShift);
