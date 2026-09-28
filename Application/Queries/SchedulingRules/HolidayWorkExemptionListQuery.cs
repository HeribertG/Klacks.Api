// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

using Klacks.Api.Application.DTOs.Scheduling;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Queries.SchedulingRules;

public sealed record HolidayWorkExemptionListQuery : IRequest<List<HolidayWorkExemptionResource>>;
