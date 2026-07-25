// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Application.Queries;
using Klacks.Api.Application.DTOs.Schedules;
using Klacks.Api.Infrastructure.Mediator;
using Microsoft.AspNetCore.Mvc;

namespace Klacks.Api.Presentation.Controllers.UserBackend.Schedules;

public class IndividualPeriodsController : InputBaseController<IndividualPeriodResource>
{
    private readonly ILogger<IndividualPeriodsController> _logger;

    public IndividualPeriodsController(IMediator mediator, ILogger<IndividualPeriodsController> logger)
      : base(mediator, logger)
    {
        this._logger = logger;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<IndividualPeriodResource>>> List() => this.Ok(await this.Mediator.Send(new ListQuery<IndividualPeriodResource>()));
}
