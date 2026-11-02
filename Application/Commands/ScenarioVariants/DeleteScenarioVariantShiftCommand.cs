// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Command to delete one shift of a variant (key) together with its whole order family. Answers false when the variant
/// or the shift is not found.
/// </summary>
/// <param name="VariantId">Row id of the variant that carries the shift</param>
/// <param name="ShiftId">Id of any row of the shift family: sealed order, plannable copy or cut piece</param>

using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Commands.ScenarioVariants;

public record DeleteScenarioVariantShiftCommand(Guid VariantId, Guid ShiftId) : IRequest<bool>;
