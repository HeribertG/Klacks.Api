// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// What the staleness check reads from one walk along a scenario's SourceToken chain.
/// </summary>
/// <param name="EarliestCreation">Instant of the snapshot the scenario's rows stand for: the earliest creation along the chain</param>
/// <param name="RootIsRealPlanCover">The root of the chain is the cover of an absence built on the real plan, or the root could not be reached</param>

namespace Klacks.Api.Infrastructure.Services.AnalyseScenarios;

internal sealed record ScenarioSourceChain(DateTime EarliestCreation, bool RootIsRealPlanCover);
