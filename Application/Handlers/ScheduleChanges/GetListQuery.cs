// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Application.DTOs.Schedules;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Handlers.ScheduleChanges;

public class GetListQuery : IRequest<List<ScheduleChangeResource>>
{
    public DateOnly StartDate { get; set; }
    public DateOnly EndDate { get; set; }
}
