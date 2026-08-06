// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Application.DTOs.Scheduling;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Commands.SchedulingRules;

/// <summary>
/// Creates a permission to work on statutory holidays.
/// </summary>
/// <param name="Resource">Description and optional scheduling-rule scope of the exemption</param>
public sealed record PostHolidayWorkExemptionCommand(HolidayWorkExemptionResource Resource)
    : IRequest<HolidayWorkExemptionResource>;
