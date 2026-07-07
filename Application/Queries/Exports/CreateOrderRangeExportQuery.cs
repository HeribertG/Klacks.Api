// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Query to generate a ZIP archive containing one export file per sealed order plus a
/// client period export for the requested date range, restricted to period-closed entries.
/// @param Filter - Contains the date range, per-order format key and localization settings
/// </summary>
using Klacks.Api.Application.DTOs.Exports;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Queries.Exports;

public record CreateOrderRangeExportQuery(OrderRangeExportFilter Filter) : IRequest<OrderExportResult>;
