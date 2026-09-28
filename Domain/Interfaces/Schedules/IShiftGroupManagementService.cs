// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

using Klacks.Api.Domain.Models.Associations;

namespace Klacks.Api.Domain.Interfaces.Schedules;

public interface IShiftGroupManagementService
{
    Task UpdateGroupItemsAsync(Guid shiftId, List<Guid> actualGroupIds);

    Task<List<Group>> GetGroupsForShiftAsync(Guid shiftId);
}