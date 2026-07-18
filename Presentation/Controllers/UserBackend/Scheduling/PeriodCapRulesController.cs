// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Application.DTOs.Scheduling;
using Klacks.Api.Application.Queries;
using Klacks.Api.Infrastructure.Mediator;
using Microsoft.AspNetCore.Mvc;

namespace Klacks.Api.Presentation.Controllers.UserBackend.Scheduling;

public class PeriodCapRulesController : InputBaseController<PeriodCapRuleResource>
{
    private readonly ILogger<PeriodCapRulesController> _logger;

    public PeriodCapRulesController(IMediator mediator, ILogger<PeriodCapRulesController> logger)
        : base(mediator, logger)
    {
        _logger = logger;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<PeriodCapRuleResource>>> GetAll()
    {
        var rules = await Mediator.Send(new ListQuery<PeriodCapRuleResource>());
        return Ok(rules);
    }
}
