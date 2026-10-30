// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// A non-blocking schedule finding of a date assign_container_week books anyway.
/// </summary>
/// <param name="Date">The booked calendar date</param>
/// <param name="Severity">Severity of the finding as the pre-commit check reports it</param>
/// <param name="Comment">Key of the finding</param>

namespace Klacks.Api.Application.Services.Schedules;

public sealed record ContainerWeekWarning(DateOnly Date, string Severity, string Comment);
