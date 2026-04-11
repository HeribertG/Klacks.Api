// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Application.DTOs.PeriodClosing;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Queries.PeriodClosing;

/// <summary>
/// Query for retrieving export log entries within a date range.
/// </summary>
/// <param name="From">Start of the date range (inclusive)</param>
/// <param name="To">End of the date range (inclusive)</param>
public record GetExportLogQuery(DateOnly From, DateOnly To) : IRequest<List<ExportLogDto>>;
