// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

using Klacks.Api.Application.DTOs.Grouping;

namespace Klacks.Api.Application.Interfaces.Grouping;

public interface IGroupingFeasibilityDataSource
{
    Task<GroupingFeasibilitySnapshot> LoadAsync(DateOnly from, DateOnly until, DateOnly today, CancellationToken cancellationToken);

    Task<bool> HasAnalysableDataAsync(DateOnly today, CancellationToken cancellationToken);
}
