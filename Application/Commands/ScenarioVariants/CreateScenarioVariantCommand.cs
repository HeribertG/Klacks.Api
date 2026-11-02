// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Command to create a new variant (key) that carries not yet active shifts.
/// </summary>
/// <param name="Request">Contains name, description, group and time period of the variant</param>

using Klacks.Api.Application.DTOs.Schedules;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Commands.ScenarioVariants;

public record CreateScenarioVariantCommand(CreateScenarioVariantRequest Request) : IRequest<ScenarioVariantResource>;
