using Klacks.Api.Domain.Models.Settings;
using Klacks.Api.Presentation.DTOs.Filter;
using Klacks.Api.Presentation.DTOs.Settings;
using Klacks.Api.Infrastructure.Mediator;
using Klacks.Api.Infrastructure.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace Klacks.Api.Presentation.Controllers.UserBackend;

public class CalendarRulesController : BaseController
{
    private readonly IMediator mediator;
    private readonly IHolidayCalculatorCache _holidayCache;

    public CalendarRulesController(IMediator mediator, IHolidayCalculatorCache holidayCache, ILogger<CalendarRulesController> logger)
    {
        this.mediator = mediator;
        _holidayCache = holidayCache;
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

    [HttpPost("ValidateCalendarRule")]
    public async Task<ActionResult<ValidateCalendarRuleResponse>> ValidateCalendarRule([FromBody] ValidateCalendarRuleRequest request)
    {
        var result = await mediator.Send(new Application.Queries.Settings.CalendarRules.ValidateRuleQuery(
            request.Rule,
            request.SubRule,
            request.Year));
        return Ok(result);
    }

    [HttpDelete("InvalidateHolidayCache/{calendarSelectionId}")]
    public ActionResult InvalidateHolidayCache(Guid calendarSelectionId)
    {
        _holidayCache.Invalidate(calendarSelectionId);
        return Ok(new { message = $"Holiday cache invalidated for CalendarSelectionId: {calendarSelectionId}" });
    }

    [HttpDelete("InvalidateAllHolidayCache")]
    public ActionResult InvalidateAllHolidayCache()
    {
        _holidayCache.InvalidateAll();
        return Ok(new { message = "All holiday caches invalidated" });
    }
}
