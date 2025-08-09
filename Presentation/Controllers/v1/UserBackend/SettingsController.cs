using Klacks.Api.Commands.Settings.Settings;
using Klacks.Api.Models.Settings;
using Klacks.Api.Application.Queries;
using Klacks.Api.Presentation.DTOs.Filter;
using Klacks.Api.Presentation.DTOs.Schedules;
using Klacks.Api.Presentation.DTOs.Settings;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using System.Text;

namespace Klacks.Api.Presentation.Controllers.v1.UserBackend;

public class SettingsController : BaseController
{
    private readonly ILogger<SettingsController> logger;
    private readonly IMediator mediator;

    public SettingsController(IMediator mediator, ILogger<SettingsController> logger)
    {
        this.mediator = mediator;
        this.logger = logger;
    }

    #region Settings

    [HttpPost("AddSetting")]
    public async Task<Models.Settings.Settings> AddSetting([FromBody] Models.Settings.Settings setting)
    {
        try
        {
            logger.LogInformation("Adding new setting.");
            var res = await mediator.Send(new PostCommand(setting));
            logger.LogInformation("Setting added successfully.");
            return res;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error occurred while adding setting.");
            throw;
        }
    }

    [HttpGet("GetSetting/{type}")]
    public async Task<ActionResult<Models.Settings.Settings?>> GetSetting(string type)
    {
        try
        {
            logger.LogInformation($"Fetching setting of type: {type}");
            var setting = await mediator.Send(new Application.Queries.Settings.Settings.GetQuery(type));
            if (setting == null)
            {
                logger.LogWarning($"Setting of type: {type} not found.");
                return NotFound();
            }

            return setting;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, $"Error occurred while fetching setting of type: {type}");
            throw;
        }
    }

    [HttpGet("GetSettingsList")]
    public async Task<IEnumerable<Models.Settings.Settings>> GetSettingsListAsync()
    {
        try
        {
            logger.LogInformation("Fetching settings list.");
            var settings = await mediator.Send(new Application.Queries.Settings.Settings.ListQuery());
            logger.LogInformation($"Retrieved {settings.Count()} settings.");
            return settings;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error occurred while fetching settings list.");
            throw;
        }
    }

    [HttpPut("PutSetting")]
    public async Task<Models.Settings.Settings> PutSetting([FromBody] Models.Settings.Settings setting)
    {
        try
        {
            logger.LogInformation("Updating setting.");
            var res = await mediator.Send(new PutCommand(setting));
            logger.LogInformation("Setting updated successfully.");
            return res;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error occurred while updating setting.");
            throw;
        }
    }

    #endregion Settings

    #region MacrosType

    [HttpDelete("MacroType/{id}")]
    public async Task<ActionResult<MacroType>> DeleteMacroType(Guid id)
    {
        var macroType = await mediator.Send(new Commands.Settings.MacrosTypes.DeleteCommand(id));
        if (macroType == null)
        {
            return NotFound();
        }

        return macroType;
    }

    [HttpGet("MacroType")]
    public async Task<IEnumerable<MacroType>> GetMacroType()
    {
        return await mediator.Send(new Application.Queries.Settings.MacrosTypes.ListQuery());
    }

    [HttpGet("MacroType/{id}")]
    public async Task<ActionResult<MacroType>> GetMacroType(Guid id)
    {
        var macroType = await mediator.Send(new Application.Queries.Settings.MacrosTypes.GetQuery(id));

        if (macroType == null)
        {
            return NotFound();
        }

        return macroType;
    }

    [HttpPost("MacroType")]
    public async Task<MacroType> PostMacroType([FromBody] MacroType macroType)
    {
        return await mediator.Send(new Commands.Settings.MacrosTypes.PostCommand(macroType));
    }

    [HttpPut("MacroType")]
    public async Task<MacroType> PutMacroType([FromBody] MacroType macroType)
    {
        return await mediator.Send(new Commands.Settings.MacrosTypes.PutCommand(macroType));
    }

    #endregion MacrosType

    #region Macros

    [HttpDelete("Macros/{id}")]
    public async Task<ActionResult<MacroResource>> DeleteMacro(Guid id)
    {
        return await mediator.Send(new Commands.Settings.Macros.DeleteCommand(id));
    }

    [HttpGet("Macros")]
    public async Task<IEnumerable<MacroResource>> GetMacro()
    {
        return await mediator.Send(new Application.Queries.Settings.Macros.ListQuery());
    }

    [HttpGet("Macros/{id}")]
    public async Task<ActionResult<MacroResource>> GetMacro(Guid id)
    {
        var macro = await mediator.Send(new Application.Queries.Settings.Macros.GetQuery(id));

        if (macro == null)
        {
            return NotFound();
        }

        return macro;
    }

    [HttpPost("Macros")]
    public async Task<ActionResult<MacroResource>> PostMacro([FromBody] MacroResource macroResource)
    {
        return await mediator.Send(new Commands.Settings.Macros.PostCommand(macroResource));
    }

    [HttpPut("Macros")]
    public async Task<ActionResult<MacroResource>> PutMacro([FromBody] MacroResource macroResource)
    {
        return await mediator.Send(new Commands.Settings.Macros.PutCommand(macroResource));
    }

    #endregion Macros

    #region Vat

    [HttpDelete("Vat/{id}")]
    public async Task<ActionResult<Vat>> DeleteVAT(Guid id)
    {
        var vat = await mediator.Send(new Commands.Settings.Vats.DeleteCommand(id));
        if (vat == null)
        {
            return NotFound();
        }

        return vat;
    }

    [HttpGet("Vat")]
    public async Task<IEnumerable<Vat>> GetVAT()
    {
        return await mediator.Send(new Application.Queries.Settings.Vats.ListQuery());
    }

    [HttpGet("Vat/{id}")]
    public async Task<ActionResult<Vat>> GetVAT(Guid id)
    {
        var vat = await mediator.Send(new Application.Queries.Settings.Vats.GetQuery(id));

        if (vat == null)
        {
            return NotFound();
        }

        return vat;
    }

    [HttpPost("Vat")]
    public async Task<ActionResult<Vat>> PostVAT([FromBody] Vat vat)
    {
        return await mediator.Send(new Commands.Settings.Vats.PostCommand(vat));
    }

    [HttpPut("Vat")]
    public async Task<ActionResult<Vat>> PutVAT([FromBody] Vat vat)
    {
        return await mediator.Send(new Commands.Settings.Vats.PutCommand(vat));
    }

    #endregion Vat

    #region CalendarRule
        
    [HttpDelete("CalendarRule/{id}")]
    public async Task<ActionResult<CalendarRule>> DeleteCalendarRule(Guid id)
    {
        var calendarRule = await mediator.Send(new Commands.Settings.CalendarRules.DeleteCommand(id));
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
        return await mediator.Send(new Application.Queries.Settings.CalendarRules.ListQuery());
    }

    [HttpGet("GetRuleTokenList")]
    public async Task<IEnumerable<StateCountryToken>> GetRuleTokenList(bool isSelected)
    {
        return await mediator.Send(new Application.Queries.Settings.CalendarRules.RuleTokenList(isSelected));
    }

    [HttpPost("GetSimpleCalendarRuleList")]
    public async Task<TruncatedCalendarRule> GetSimpleCalendarRuleList([FromBody] CalendarRulesFilter filter)
    {
        var tmp = JsonConvert.SerializeObject(filter);
        return await mediator.Send(new Application.Queries.Settings.CalendarRules.TruncatedListQuery(filter));
    }

    [HttpPost("CalendarRule")]
    public async Task<ActionResult<CalendarRule>> PostCalendarRule([FromBody] CalendarRuleResource calendarRuleResource)
    {
        var calendarRule = await mediator.Send(new Commands.Settings.CalendarRules.PostCommand(calendarRuleResource));

        return Ok(calendarRule);
    }

    [HttpPut("CalendarRule")]
    public async Task<ActionResult<CalendarRule>> PutCalendarRule([FromBody] CalendarRule calendarRule)
    {
        await mediator.Send(new Commands.Settings.CalendarRules.PutCommand(calendarRule));

        return calendarRule;
    }

    #endregion CalendarRule
}
