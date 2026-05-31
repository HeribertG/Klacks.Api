// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Domain.Enums;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Commands.Qualifications;

/// <summary>
/// Upserts a shift's required qualification: if an active row for (ShiftId, QualificationId) exists
/// its IsMandatory / MinLevel are updated, otherwise a new row is created. Upsert is required because
/// the partial unique index forbids two active rows with the same key. Returns the row id.
/// </summary>
/// <param name="ShiftId">Shift that requires the qualification</param>
/// <param name="QualificationId">The required qualification</param>
/// <param name="IsMandatory">True = an employee without it (at MinLevel) is ineligible; false = soft signal</param>
/// <param name="MinLevel">Minimum required level (Low..Expert)</param>
public record SetShiftRequiredQualificationCommand(
    Guid ShiftId,
    Guid QualificationId,
    bool IsMandatory,
    QualificationLevel MinLevel) : IRequest<Guid>;
