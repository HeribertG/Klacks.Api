// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Application.DTOs.Schedules;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Queries.Schedules;

/// <summary>
/// Asks for the read-only report over the captured wizard runs.
/// </summary>
/// <param name="From">Lower period bound; null for no lower bound.</param>
/// <param name="Until">Upper period bound; null for no upper bound.</param>
/// <param name="GroupId">Restrict to one group; null for every group.</param>
public sealed record GetWizardRunCaptureReportQuery(DateOnly? From, DateOnly? Until, Guid? GroupId)
    : IRequest<WizardRunCaptureReportDto>;
