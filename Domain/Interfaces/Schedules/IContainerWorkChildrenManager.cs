// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Manages the business logic for updating container work children (sub-works, sub-breaks, work changes),
/// including macro execution and surcharge calculation.
/// </summary>
/// <param name="workId">The parent container work ID</param>
using Klacks.Api.Domain.Models.Schedules;

namespace Klacks.Api.Domain.Interfaces.Schedules;

public interface IContainerWorkChildrenManager
{
    Task<Work?> UpdateChildrenAsync(
        Guid workId,
        string? parentStartBase,
        string? parentEndBase,
        TimeOnly? parentStartTime,
        TimeOnly? parentEndTime,
        List<Work> updatedWorks,
        List<Break> updatedBreaks,
        List<WorkChange> updatedWorkChanges,
        CancellationToken cancellationToken);
}
