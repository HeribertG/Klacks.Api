// Copyright (c) Heribert Gasparoli Private. All rights reserved.

namespace Klacks.Api.Domain.Services.ShiftSchedule;

public interface IShiftGroupFilterService
{
    Task<List<Guid>> GetVisibleGroupIdsAsync(Guid? selectedGroupId);
}
