// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Application.Queries;
using Klacks.Api.Application.DTOs.Schedules;
using Klacks.Api.Infrastructure.Mediator;
using Microsoft.AspNetCore.Mvc;

namespace Klacks.Api.Presentation.Controllers.UserBackend.Schedules;

public class MonthlyTargetHoursController : InputBaseController<MonthlyTargetHoursResource>
{
    private readonly ILogger<MonthlyTargetHoursController> _logger;

    public MonthlyTargetHoursController(IMediator mediator, ILogger<MonthlyTargetHoursController> logger)
      : base(mediator, logger)
    {
        this._logger = logger;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<MonthlyTargetHoursResource>>> List() => this.Ok(await this.Mediator.Send(new ListQuery<MonthlyTargetHoursResource>()));
}
