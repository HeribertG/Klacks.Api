// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Command to delete an unused variant (key) together with its shifts.
/// </summary>
/// <param name="Id">Row id of the variant to delete</param>

using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Commands.ScenarioVariants;

public record DeleteScenarioVariantCommand(Guid Id) : IRequest<bool>;
