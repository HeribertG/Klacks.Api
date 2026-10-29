// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

using Klacks.Api.Application.DTOs.Schedules;
using Klacks.Api.Application.Services.Schedules;

namespace Klacks.Api.Application.Interfaces.Schedules;

public interface IContainerRouteWriteService
{
    Task<ContainerRouteWriteResult> OptimizeAndWriteAsync(
        Guid containerId, int storageWeekday, bool isHoliday, string instanceId, CancellationToken cancellationToken);

    /// <summary>
    /// Same as OptimizeAndWriteAsync for a caller that already holds the container template lock; refuses without
    /// writing when lockScope has not acquired it.
    /// </summary>
    Task<ContainerRouteWriteResult> OptimizeAndWriteUnderHeldLockAsync(
        ContainerTemplateLockScope lockScope, Guid containerId, int storageWeekday, bool isHoliday,
        CancellationToken cancellationToken);
}
