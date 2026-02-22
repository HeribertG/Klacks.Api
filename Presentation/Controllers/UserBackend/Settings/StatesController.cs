// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Application.Queries;
using Klacks.Api.Application.DTOs.Settings;
using Klacks.Api.Infrastructure.Mediator;
using Microsoft.AspNetCore.Mvc;

namespace Klacks.Api.Presentation.Controllers.UserBackend.Settings;

public class StatesController : InputBaseController<StateResource>
{
    private readonly ILogger<StatesController> _logger;

    public StatesController(IMediator mediator, ILogger<StatesController> logger)
      : base(mediator, logger)
    {
        this._logger = logger;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<StateResource>>> List() => this.Ok(await this.Mediator.Send(new ListQuery<StateResource>()));
}
