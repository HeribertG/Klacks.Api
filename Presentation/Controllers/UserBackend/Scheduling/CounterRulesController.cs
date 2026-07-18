// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Application.DTOs.Scheduling;
using Klacks.Api.Application.Queries;
using Klacks.Api.Infrastructure.Mediator;
using Microsoft.AspNetCore.Mvc;

namespace Klacks.Api.Presentation.Controllers.UserBackend.Scheduling;

public class CounterRulesController : InputBaseController<CounterRuleResource>
{
    private readonly ILogger<CounterRulesController> _logger;

    public CounterRulesController(IMediator mediator, ILogger<CounterRulesController> logger)
        : base(mediator, logger)
    {
        _logger = logger;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<CounterRuleResource>>> GetAll()
    {
        var rules = await Mediator.Send(new ListQuery<CounterRuleResource>());
        return Ok(rules);
    }
}
