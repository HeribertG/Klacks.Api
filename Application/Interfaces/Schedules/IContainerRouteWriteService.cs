// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

using Klacks.Api.Application.DTOs.Schedules;

namespace Klacks.Api.Application.Interfaces.Schedules;

public interface IContainerRouteWriteService
{
    Task<ContainerRouteWriteResult> OptimizeAndWriteAsync(
        Guid containerId, int storageWeekday, bool isHoliday, string instanceId, CancellationToken cancellationToken);
}
