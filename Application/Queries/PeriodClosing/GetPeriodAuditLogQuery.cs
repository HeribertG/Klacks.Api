// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

using Klacks.Api.Application.DTOs.PeriodClosing;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Queries.PeriodClosing;

/// <summary>
/// Query for retrieving audit log entries for seal and unseal actions within a date range.
/// </summary>
/// <param name="From">Start of the date range (inclusive)</param>
/// <param name="To">End of the date range (inclusive)</param>
public record GetPeriodAuditLogQuery(DateOnly From, DateOnly To) : IRequest<List<PeriodAuditLogDto>>;
