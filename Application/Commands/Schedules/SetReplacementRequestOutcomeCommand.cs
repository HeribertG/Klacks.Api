// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

using Klacks.Api.Application.DTOs.Schedules;
using Klacks.Api.Domain.Enums;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Commands.Schedules;

/// <summary>
/// Records the candidate's answer on an existing replacement request row (the three buttons in the recovery
/// dialog); Accepted also makes the candidate the slot's replacement in the row's scenario. A row whose candidate the
/// caller may not see is answered like a missing one.
/// </summary>
/// <param name="Id">Row id</param>
/// <param name="Outcome">Requested, Accepted, Declined or NotReached</param>
/// <param name="OverrideBlock">K1 supervisor override for a Block-mode compliance exclusion of the candidate</param>
public record SetReplacementRequestOutcomeCommand(Guid Id, ReplacementRequestOutcome Outcome, bool OverrideBlock = false)
    : IRequest<ReplacementRequestResource>;
