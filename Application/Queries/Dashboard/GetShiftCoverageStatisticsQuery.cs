// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Query zum Abrufen der Shift-Abdeckungs- und Versiegelungsstatistiken pro Gruppe für den aktuellen Monat.
/// </summary>
using Klacks.Api.Application.DTOs.Dashboard;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Queries.Dashboard;

public record GetShiftCoverageStatisticsQuery : IRequest<IEnumerable<ShiftCoverageStatisticsResource>>;
