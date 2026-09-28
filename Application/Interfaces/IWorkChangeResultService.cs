// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

using Klacks.Api.Application.DTOs.Schedules;

namespace Klacks.Api.Application.Interfaces;

public interface IWorkChangeResultService
{
    Task<WorkChangeClientResult> GetClientResultAsync(
        Guid clientId,
        DateOnly periodStart,
        DateOnly periodEnd,
        DateOnly threeDayStart,
        DateOnly threeDayEnd,
        CancellationToken cancellationToken,
        Guid? analyseToken = null);
}
