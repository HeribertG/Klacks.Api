// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

using Klacks.Api.Domain.Interfaces.Schedules;
using Klacks.Api.Domain.Models.Associations;

namespace Klacks.Api.Domain.Interfaces.Schedules;

public interface IShiftValidator
{
    void EnsureUniqueGroupItems(ICollection<GroupItem> groupItems);
    Task EnsureNoOriginalShiftCopyExists(Guid originalShiftId, IShiftRepository shiftRepository, Guid? analyseToken = null);
    Task ValidateCutDatesWithinAllowedRange(
        Guid originalId,
        DateOnly fromDate,
        DateOnly? untilDate,
        IShiftRepository shiftRepository,
        Guid? analyseToken = null);
}
