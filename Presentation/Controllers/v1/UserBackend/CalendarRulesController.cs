using Klacks.Api.Domain.Models.Settings;
using Klacks.Api.Presentation.DTOs.Filter;
using Klacks.Api.Presentation.DTOs.Settings;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Klacks.Api.Presentation.Controllers.v1.UserBackend;

public class CalendarRulesController : BaseController
{
    private readonly IMediator mediator;

    public CalendarRulesController(IMediator mediator, ILogger<CalendarRulesController> logger)
    {
        this.mediator = mediator;
    }

    [HttpDelete("CalendarRule/{id}")]
    public async Task<ActionResult<CalendarRule>> DeleteCalendarRule(Guid id)
    {
        var calendarRule = await mediator.Send(new Application.Commands.Settings.CalendarRules.DeleteCommand(id));
        if (calendarRule == null)
        {
            return NotFound();
        }
        return calendarRule;
    }

    [HttpGet("CalendarRule/{id}")]
    public async Task<ActionResult<CalendarRule>> GetCalendarRule(Guid id)
    {
        var calendarRule = await mediator.Send(new Application.Queries.Settings.CalendarRules.GetQuery(id));

        if (calendarRule == null)
        {
            return NotFound();
        }
        return calendarRule;
    }

    [HttpGet("GetCalendarRuleList")]
    public async Task<IEnumerable<CalendarRule>> GetCalendarRuleList()
    {
        var result = await mediator.Send(new Application.Queries.Settings.CalendarRules.ListQuery());
        return result;
    }

    [HttpGet("GetRuleTokenList")]
    public async Task<IEnumerable<StateCountryToken>> GetRuleTokenList(bool isSelected)
    {
        var result = await mediator.Send(new Application.Queries.Settings.CalendarRules.RuleTokenList(isSelected));
        return result;
    }

    [HttpPost("GetSimpleCalendarRuleList")]
    public async Task<TruncatedCalendarRule> GetSimpleCalendarRuleList([FromBody] CalendarRulesFilter filter)
    {
        var result = await mediator.Send(new Application.Queries.Settings.CalendarRules.TruncatedListQuery(filter));
        return result;
    }

    [HttpPost("CalendarRule")]
    public async Task<ActionResult<CalendarRule>> PostCalendarRule([FromBody] CalendarRuleResource calendarRuleResource)
    {
        var calendarRule = await mediator.Send(new Application.Commands.Settings.CalendarRules.PostCommand(calendarRuleResource));
        return Ok(calendarRule);
    }

    [HttpPut("CalendarRule")]
    public async Task<ActionResult<CalendarRule>> PutCalendarRule([FromBody] CalendarRule calendarRule)
    {
        await mediator.Send(new Application.Commands.Settings.CalendarRules.PutCommand(calendarRule));
        return calendarRule;
    }
}
