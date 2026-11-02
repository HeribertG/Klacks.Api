// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Query for the services (SealedOrder shifts) a variant (key) carries.
/// </summary>
/// <param name="Id">Row id of the variant</param>

using Klacks.Api.Application.DTOs.Schedules;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Queries.ScenarioVariants;

public record GetScenarioVariantShiftsQuery(Guid Id) : IRequest<List<ShiftResource>?>;
