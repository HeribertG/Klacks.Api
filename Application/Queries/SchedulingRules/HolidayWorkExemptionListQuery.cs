// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Application.DTOs.Scheduling;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Queries.SchedulingRules;

public sealed record HolidayWorkExemptionListQuery : IRequest<List<HolidayWorkExemptionResource>>;
