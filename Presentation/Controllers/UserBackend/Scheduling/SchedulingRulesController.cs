// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Application.Commands.SchedulingRules;
using Klacks.Api.Application.Queries;
using Klacks.Api.Application.DTOs.Scheduling;
using Klacks.Api.Application.Queries.SchedulingRules;
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
    public async Task<ActionResult<IEnumerable<SchedulingRuleResource>>> GetAll([FromQuery] bool activeIndustriesOnly = false)
    {
        var rules = activeIndustriesOnly
            ? await Mediator.Send(new SelectionListQuery())
            : await Mediator.Send(new ListQuery<SchedulingRuleResource>());
        return Ok(rules);
    }

    /// <summary>
    /// Contracts that still reference a rule of an industry which is no longer active. Switching
    /// ACTIVE_INDUSTRIES leaves those assignments in place on purpose, so this is the list an admin
    /// works through to migrate them deliberately.
    /// </summary>
    [HttpGet("IndustryMigration")]
    public async Task<ActionResult<IEnumerable<IndustryMigrationCandidate>>> GetIndustryMigrationList()
    {
        return Ok(await Mediator.Send(new IndustryMigrationListQuery()));
    }

    /// <summary>
    /// The permissions to work on statutory holidays. Without at least one matching exemption, every
    /// staffed shift on a public holiday is reported.
    /// </summary>
    [HttpGet("HolidayWorkExemptions")]
    public async Task<ActionResult<IEnumerable<HolidayWorkExemptionResource>>> GetHolidayWorkExemptions()
    {
        return Ok(await Mediator.Send(new HolidayWorkExemptionListQuery()));
    }

    [HttpPost("HolidayWorkExemptions")]
    public async Task<ActionResult<HolidayWorkExemptionResource>> CreateHolidayWorkExemption(
        [FromBody] HolidayWorkExemptionResource resource)
    {
        return Ok(await Mediator.Send(new PostHolidayWorkExemptionCommand(resource)));
    }

    [HttpDelete("HolidayWorkExemptions/{id}")]
    public async Task<ActionResult> DeleteHolidayWorkExemption(Guid id)
    {
        return await Mediator.Send(new DeleteHolidayWorkExemptionCommand(id)) ? Ok() : NotFound();
    }

    /// <summary>
    /// Applies the migration decisions the admin made on that list.
    /// </summary>
    [HttpPut("IndustryMigration")]
    public async Task<ActionResult<int>> MigrateContracts([FromBody] List<ContractSchedulingRuleAssignment> assignments)
    {
        if (assignments == null || assignments.Count == 0)
        {
            return BadRequest("At least one assignment is required.");
        }

        return Ok(await Mediator.Send(new MigrateContractSchedulingRulesCommand(assignments)));
    }
}
