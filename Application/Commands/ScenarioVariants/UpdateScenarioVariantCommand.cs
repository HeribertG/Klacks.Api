// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Command to change name, description and time period of an unused variant (key).
/// </summary>
/// <param name="Id">Row id of the variant to change</param>
/// <param name="Request">Contains the new name, description and time period</param>

using Klacks.Api.Application.DTOs.Schedules;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Commands.ScenarioVariants;

public record UpdateScenarioVariantCommand(Guid Id, UpdateScenarioVariantRequest Request) : IRequest<ScenarioVariantResource>;
