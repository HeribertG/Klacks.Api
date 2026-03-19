// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Domain.Interfaces.Schedules;
using Klacks.Api.Domain.Models.Associations;

namespace Klacks.Api.Domain.Interfaces.Schedules;

public interface IShiftValidator
{
    void EnsureUniqueGroupItems(ICollection<GroupItem> groupItems);
    Task EnsureNoOriginalShiftCopyExists(Guid originalShiftId, IShiftRepository shiftRepository);
    Task ValidateCutDatesWithinAllowedRange(
        Guid originalId,
        DateOnly fromDate,
        DateOnly? untilDate,
        IShiftRepository shiftRepository);
}
