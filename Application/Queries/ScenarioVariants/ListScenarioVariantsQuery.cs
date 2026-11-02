// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Query for the variants (keys) of one group, consumed ones included.
/// </summary>
/// <param name="GroupId">Group whose variants are loaded</param>

using Klacks.Api.Application.DTOs.Schedules;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Queries.ScenarioVariants;

public record ListScenarioVariantsQuery(Guid GroupId) : IRequest<List<ScenarioVariantResource>>;
