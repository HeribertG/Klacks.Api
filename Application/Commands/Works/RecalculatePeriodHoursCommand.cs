// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

using Klacks.Api.Application.DTOs.Schedules;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Commands.Works;

/// <summary>
/// Recalculates the cached period hours of all clients (of the selected group) for every pay period the window touches.
/// Frozen periods are skipped and counted unless ConfirmFrozen is set with a reason (admin override, audited).
/// </summary>
/// <param name="StartDate">Inclusive window start</param>
/// <param name="EndDate">Inclusive window end</param>
/// <param name="SelectedGroup">Optional group scope; when null all clients are processed</param>
/// <param name="ConfirmFrozen">Admin override: recalculate frozen periods too</param>
/// <param name="Reason">Mandatory with ConfirmFrozen; stored in the period audit log</param>
public record RecalculatePeriodHoursCommand(
    DateOnly StartDate,
    DateOnly EndDate,
    Guid? SelectedGroup = null,
    bool ConfirmFrozen = false,
    string? Reason = null) : IRequest<PeriodHoursRecalculationResult>;
