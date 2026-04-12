// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Application.DTOs.PeriodClosing;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Queries.PeriodClosing;

/// <summary>
/// Query for listing all billing periods that are actually populated with
/// non-deleted work or break entries. Used to drive the period dropdown
/// in the period-closing UI instead of free-form date range input.
/// </summary>
public record GetUsedPeriodsQuery() : IRequest<List<UsedPeriodDto>>;
