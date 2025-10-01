using Klacks.Api.Domain.Models.Settings;
using Klacks.Api.Presentation.DTOs.Filter;
using Klacks.Api.Presentation.DTOs.Settings;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;

namespace Klacks.Api.Presentation.Controllers.v1.UserBackend;

public class CalendarRulesController : BaseController
{
    private readonly ILogger<CalendarRulesController> logger;
    private readonly IMediator mediator;

    public CalendarRulesController(IMediator mediator, ILogger<CalendarRulesController> logger)
    {
        this.mediator = mediator;
        this.logger = logger;
    }

    [HttpDelete("CalendarRule/{id}")]
    public async Task<ActionResult<CalendarRule>> DeleteCalendarRule(Guid id)
    {
        logger.LogInformation($"Deleting CalendarRule with ID: {id}");
        var calendarRule = await mediator.Send(new Application.Commands.Settings.CalendarRules.DeleteCommand(id));
        if (calendarRule == null)
        {
            logger.LogWarning($"CalendarRule with ID: {id} not found");
            return NotFound();
        }
        logger.LogInformation($"CalendarRule with ID: {id} deleted successfully");
        return calendarRule;
    }

    [HttpGet("CalendarRule/{id}")]
    public async Task<ActionResult<CalendarRule>> GetCalendarRule(Guid id)
    {
        logger.LogInformation($"Fetching CalendarRule with ID: {id}");
        var calendarRule = await mediator.Send(new Application.Queries.Settings.CalendarRules.GetQuery(id));

        if (calendarRule == null)
        {
            logger.LogWarning($"CalendarRule with ID: {id} not found");
            return NotFound();
        }
        logger.LogInformation($"CalendarRule with ID: {id} retrieved successfully");
        return calendarRule;
    }

    [HttpGet("GetCalendarRuleList")]
    public async Task<IEnumerable<CalendarRule>> GetCalendarRuleList()
    {
        logger.LogInformation("Fetching CalendarRule list");
        var result = await mediator.Send(new Application.Queries.Settings.CalendarRules.ListQuery());
        logger.LogInformation($"Retrieved {result.Count()} CalendarRules");
        return result;
    }

    [HttpGet("GetRuleTokenList")]
    public async Task<IEnumerable<StateCountryToken>> GetRuleTokenList(bool isSelected)
    {
        logger.LogInformation($"Fetching RuleToken list with isSelected: {isSelected}");
        var result = await mediator.Send(new Application.Queries.Settings.CalendarRules.RuleTokenList(isSelected));
        logger.LogInformation($"Retrieved {result.Count()} RuleTokens");
        return result;
    }

    [HttpPost("GetSimpleCalendarRuleList")]
    public async Task<TruncatedCalendarRule> GetSimpleCalendarRuleList([FromBody] CalendarRulesFilter filter)
    {
        logger.LogInformation("Fetching truncated CalendarRule list");
        var tmp = JsonConvert.SerializeObject(filter);
        var result = await mediator.Send(new Application.Queries.Settings.CalendarRules.TruncatedListQuery(filter));
        logger.LogInformation($"Retrieved truncated CalendarRule list with {result.CalendarRules?.Count() ?? 0} items");
        return result;
    }

    [HttpPost("CalendarRule")]
    public async Task<ActionResult<CalendarRule>> PostCalendarRule([FromBody] CalendarRuleResource calendarRuleResource)
    {
        logger.LogInformation("Creating new CalendarRule");
        var calendarRule = await mediator.Send(new Application.Commands.Settings.CalendarRules.PostCommand(calendarRuleResource));
        logger.LogInformation("CalendarRule created successfully");
        return Ok(calendarRule);
    }

    [HttpPut("CalendarRule")]
    public async Task<ActionResult<CalendarRule>> PutCalendarRule([FromBody] CalendarRule calendarRule)
    {
        logger.LogInformation("Updating CalendarRule");
        await mediator.Send(new Application.Commands.Settings.CalendarRules.PutCommand(calendarRule));
        logger.LogInformation("CalendarRule updated successfully");
        return calendarRule;
    }
}
