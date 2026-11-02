// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Command to copy existing real shifts, each with its whole order family, into an unused variant (key).
/// </summary>
/// <param name="VariantId">Row id of the variant that receives the copies</param>
/// <param name="Request">Contains either the shift ids to copy or the source group, exactly one of them</param>

using Klacks.Api.Application.DTOs.Schedules;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Commands.ScenarioVariants;

public record CopyShiftsToVariantCommand(Guid VariantId, CopyShiftsToVariantRequest Request) : IRequest<CopyShiftsToVariantResult>;
