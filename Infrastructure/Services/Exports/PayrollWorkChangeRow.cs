// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// One WorkChange of the payroll export, flattened with the data of the work that carries it. The change belongs to
/// the day of its work (Work.CurrentDate), exactly as the period hours attribute it.
/// @param WorkId - The work carrying the change
/// @param WorkClientId - The work's own employee (the original person of a replacement)
/// @param ReplaceClientId - The replacing person of a replacement change, or null
/// @param ReplaceClientIsPayrollPerson - True when the replacing person is an Employee or ExternEmp (soft-deleted included)
/// @param Date - The work's day
/// @param Type - The WorkChange type (correction, travel, briefing or replacement)
/// @param ChangeTime - Hours of the change
/// @param Surcharges - Surcharge of the change
/// @param WorkLockLevel - Lock level of the carrying work
/// @param ShiftId - Shift of the carrying work
/// </summary>
using Klacks.Api.Domain.Enums;

namespace Klacks.Api.Infrastructure.Services.Exports;

public sealed record PayrollWorkChangeRow(
    Guid WorkId,
    Guid WorkClientId,
    Guid? ReplaceClientId,
    bool ReplaceClientIsPayrollPerson,
    DateOnly Date,
    WorkChangeType Type,
    decimal ChangeTime,
    decimal Surcharges,
    WorkLockLevel WorkLockLevel,
    Guid ShiftId);
