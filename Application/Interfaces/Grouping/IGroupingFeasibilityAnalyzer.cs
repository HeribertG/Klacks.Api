// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

using Klacks.Api.Application.DTOs.Grouping;

namespace Klacks.Api.Application.Interfaces.Grouping;

public interface IGroupingFeasibilityAnalyzer
{
    Task<GroupingFeasibilityReport> AnalyzeAsync(GroupingAnalysisRequest request, CancellationToken cancellationToken);
}
