// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

using Klacks.Api.Domain.Enums;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Commands.Qualifications;

/// <summary>
/// Upserts a shift's required qualification: if an active row for (ShiftId, QualificationId) exists
/// its IsMandatory / MinLevel / LevelBindingOverride are updated, otherwise a new row is created. Upsert is required because
/// the partial unique index forbids two active rows with the same key. Returns the row id.
/// </summary>
/// <param name="ShiftId">Shift that requires the qualification</param>
/// <param name="QualificationId">The required qualification</param>
/// <param name="IsMandatory">True = an employee without it is ineligible (below MinLevel only when the level is binding); false = soft signal</param>
/// <param name="MinLevel">Minimum required level (Low..Expert)</param>
/// <param name="LevelBindingOverride">true = the minimum level is binding, false = hint only, null = as the qualification</param>
/// <param name="KeepExistingLevelBinding">True = an existing row keeps its LevelBindingOverride (caller did not specify one); a new row then takes the override of the row it inherits for that qualification, null when there is none</param>
public record SetShiftRequiredQualificationCommand(
    Guid ShiftId,
    Guid QualificationId,
    bool IsMandatory,
    QualificationLevel MinLevel,
    bool? LevelBindingOverride,
    bool KeepExistingLevelBinding = false) : IRequest<Guid>;
