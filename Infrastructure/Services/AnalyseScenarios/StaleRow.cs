// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// A real row inside an accept scope that changed after the scenario's snapshot, with what the day-lock plan needs to
/// decide whether the accept overwrites it.
/// </summary>
/// <param name="Kind">Work, child of a work (work change or expense), break or schedule note</param>
/// <param name="Id">Id of the row itself</param>
/// <param name="ClientId">Employee of the row (of its work for a work change or expense)</param>
/// <param name="CurrentDate">Day of the row (of its work for a work change or expense)</param>
/// <param name="LockLevel">Lock level of the row (of its work for a work change or expense)</param>
/// <param name="WorkId">The work itself, the parent work of a work change or expense, the paused work of a break</param>
/// <param name="CreateTime">Creation instant</param>
/// <param name="UpdateTime">Last update instant</param>
/// <param name="DeletedTime">Deletion instant</param>

using Klacks.Api.Domain.Enums;

namespace Klacks.Api.Infrastructure.Services.AnalyseScenarios;

internal sealed record StaleRow(
    StaleRowKind Kind,
    Guid Id,
    Guid ClientId,
    DateOnly CurrentDate,
    WorkLockLevel LockLevel,
    Guid? WorkId,
    DateTime? CreateTime,
    DateTime? UpdateTime,
    DateTime? DeletedTime);
