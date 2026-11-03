// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// The real-plan work a scenario work descends from.
/// </summary>
/// <param name="WorkId">The real work</param>
/// <param name="MovedFromClientId">Its origin marker</param>

namespace Klacks.Api.Infrastructure.Services.AnalyseScenarios;

internal sealed record WorkRealOrigin(Guid WorkId, Guid? MovedFromClientId);
