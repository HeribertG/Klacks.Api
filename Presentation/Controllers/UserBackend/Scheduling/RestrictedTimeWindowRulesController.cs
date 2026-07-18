// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Application.DTOs.Scheduling;
using Klacks.Api.Application.Queries;
using Klacks.Api.Infrastructure.Mediator;
using Microsoft.AspNetCore.Mvc;

namespace Klacks.Api.Presentation.Controllers.UserBackend.Scheduling;

public class RestrictedTimeWindowRulesController : InputBaseController<RestrictedTimeWindowRuleResource>
{
    private readonly ILogger<RestrictedTimeWindowRulesController> _logger;

    public RestrictedTimeWindowRulesController(IMediator mediator, ILogger<RestrictedTimeWindowRulesController> logger)
        : base(mediator, logger)
    {
        _logger = logger;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<RestrictedTimeWindowRuleResource>>> GetAll()
    {
        var rules = await Mediator.Send(new ListQuery<RestrictedTimeWindowRuleResource>());
        return Ok(rules);
    }
}
