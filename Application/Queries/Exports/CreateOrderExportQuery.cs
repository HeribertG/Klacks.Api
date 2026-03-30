// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Query to generate an order export file in the specified format.
/// @param Filter - Contains date range, format key and localization settings
/// </summary>
using Klacks.Api.Application.DTOs.Exports;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Queries.Exports;

public record CreateOrderExportQuery(OrderExportFilter Filter) : IRequest<OrderExportResult>;
