// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Fetches all active required-qualification rows for a specific shift.
/// </summary>
/// <param name="ShiftId">The shift whose required qualifications to load</param>

using Klacks.Api.Application.DTOs.Associations;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Queries.Qualifications;

public record GetShiftRequiredQualificationsQuery(Guid ShiftId) : IRequest<List<ShiftRequiredQualificationResource>>;
