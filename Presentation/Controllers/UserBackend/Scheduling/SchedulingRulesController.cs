// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Application.Queries;
using Klacks.Api.Application.DTOs.Scheduling;
using Klacks.Api.Infrastructure.Mediator;
using Microsoft.AspNetCore.Mvc;

namespace Klacks.Api.Presentation.Controllers.UserBackend.Scheduling;

public class SchedulingRulesController : InputBaseController<SchedulingRuleResource>
{
    private readonly ILogger<SchedulingRulesController> _logger;

    public SchedulingRulesController(IMediator mediator, ILogger<SchedulingRulesController> logger)
        : base(mediator, logger)
    {
        _logger = logger;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<SchedulingRuleResource>>> GetAll()
    {
        var rules = await Mediator.Send(new ListQuery<SchedulingRuleResource>());
        return Ok(rules);
    }
}
