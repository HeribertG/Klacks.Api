// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Application.DTOs.Settings;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Queries.Settings.CompanyClock;

/// <summary>
/// Query for the company's configured time zone and current calendar date, open to any authenticated
/// user (Settings themselves stay Admin-only via GeneralSettingsController).
/// </summary>
public record GetCompanyClockQuery() : IRequest<CompanyClockResource>;
