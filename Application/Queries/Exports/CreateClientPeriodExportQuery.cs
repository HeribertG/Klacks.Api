// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Query to generate a date-range based client period export file, grouped by client.
/// @param Filter - Contains date range and localization settings
/// </summary>
using Klacks.Api.Application.DTOs.Exports;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Queries.Exports;

public record CreateClientPeriodExportQuery(ClientPeriodExportFilter Filter) : IRequest<OrderExportResult>;
