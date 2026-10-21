// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Limit of the walks along a scenario's source chain (scenario built on scenario): the SourceToken chain of the
/// staleness check and the ClonedFromWorkId lineage of the accept day-lock plan stop after this many steps.
/// </summary>

namespace Klacks.Api.Infrastructure.Services.AnalyseScenarios;

internal static class ScenarioSourceChainLimits
{
    public const int MaxDepth = 16;
}
