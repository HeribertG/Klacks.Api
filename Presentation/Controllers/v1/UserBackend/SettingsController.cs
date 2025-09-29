using Klacks.Api.Application.Commands.Settings.Settings;
using Klacks.Api.Domain.Models.Settings;
using Klacks.Api.Infrastructure.Email;
using Klacks.Api.Presentation.DTOs.Filter;
using Klacks.Api.Presentation.DTOs.Settings;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;

namespace Klacks.Api.Presentation.Controllers.v1.UserBackend;

public class SettingsController : BaseController
{
    private readonly ILogger<SettingsController> logger;
    private readonly IMediator mediator;
    private readonly IEmailTestService emailTestService;

    public SettingsController(IMediator mediator, ILogger<SettingsController> logger, IEmailTestService emailTestService)
    {
        this.mediator = mediator;
        this.logger = logger;
        this.emailTestService = emailTestService;
    }

    #region Settings

    [HttpPost("AddSetting")]
    public async Task<Klacks.Api.Domain.Models.Settings.Settings> AddSetting([FromBody] Klacks.Api.Domain.Models.Settings.Settings setting)
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
    public async Task<ActionResult<Klacks.Api.Domain.Models.Settings.Settings?>> GetSetting(string type)
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
    public async Task<IEnumerable<Klacks.Api.Domain.Models.Settings.Settings>> GetSettingsListAsync()
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
    public async Task<Klacks.Api.Domain.Models.Settings.Settings> PutSetting([FromBody] Klacks.Api.Domain.Models.Settings.Settings setting)
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

    [HttpPost("TestEmailConfiguration")]
    public async Task<ActionResult<EmailTestResult>> TestEmailConfiguration([FromBody] EmailTestRequest request)
    {
        try
        {
            logger.LogInformation("Testing email configuration");
            var result = await emailTestService.TestConnectionAsync(request);
            logger.LogInformation("Email configuration test completed. Success: {Success}", result.Success);
            return Ok(result);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error occurred while testing email configuration");
            return Ok(new EmailTestResult 
            { 
                Success = false, 
                Message = "An unexpected error occurred during the test.",
                ErrorDetails = ex.Message
            });
        }
    }

    #endregion Settings

    #region MacrosType

    [HttpDelete("MacroType/{id}")]
    public async Task<ActionResult<MacroType>> DeleteMacroType(Guid id)
    {
        logger.LogInformation($"Deleting MacroType with ID: {id}");
        var macroType = await mediator.Send(new Application.Commands.Settings.MacrosTypes.DeleteCommand(id));
        if (macroType == null)
        {
            logger.LogWarning($"MacroType with ID: {id} not found");
            return NotFound();
        }
        logger.LogInformation($"MacroType with ID: {id} deleted successfully");
        return macroType;
    }

    [HttpGet("MacroType")]
    public async Task<IEnumerable<MacroType>> GetMacroType()
    {
        logger.LogInformation("Fetching MacroType list");
        var result = await mediator.Send(new Application.Queries.Settings.MacrosTypes.ListQuery());
        logger.LogInformation($"Retrieved {result.Count()} MacroTypes");
        return result;
    }

    [HttpGet("MacroType/{id}")]
    public async Task<ActionResult<MacroType>> GetMacroType(Guid id)
    {
        logger.LogInformation($"Fetching MacroType with ID: {id}");
        var macroType = await mediator.Send(new Application.Queries.Settings.MacrosTypes.GetQuery(id));

        if (macroType == null)
        {
            logger.LogWarning($"MacroType with ID: {id} not found");
            return NotFound();
        }
        logger.LogInformation($"MacroType with ID: {id} retrieved successfully");
        return macroType;
    }

    [HttpPost("MacroType")]
    public async Task<MacroType> PostMacroType([FromBody] MacroType macroType)
    {
        logger.LogInformation("Creating new MacroType");
        var result = await mediator.Send(new Application.Commands.Settings.MacrosTypes.PostCommand(macroType));
        logger.LogInformation("MacroType created successfully");
        return result;
    }

    [HttpPut("MacroType")]
    public async Task<MacroType> PutMacroType([FromBody] MacroType macroType)
    {
        logger.LogInformation("Updating MacroType");
        var result = await mediator.Send(new Application.Commands.Settings.MacrosTypes.PutCommand(macroType));
        logger.LogInformation("MacroType updated successfully");
        return result;
    }

    #endregion MacrosType

    #region Macros

    [HttpDelete("Macros/{id}")]
    public async Task<ActionResult<MacroResource>> DeleteMacro(Guid id)
    {
        logger.LogInformation($"Deleting Macro with ID: {id}");
        var result = await mediator.Send(new Application.Commands.Settings.Macros.DeleteCommand(id));
        logger.LogInformation($"Macro with ID: {id} deleted successfully");
        return result;
    }

    [HttpGet("Macros")]
    public async Task<IEnumerable<MacroResource>> GetMacro()
    {
        logger.LogInformation("Fetching Macros list");
        var result = await mediator.Send(new Application.Queries.Settings.Macros.ListQuery());
        logger.LogInformation($"Retrieved {result.Count()} Macros");
        return result;
    }

    [HttpGet("Macros/{id}")]
    public async Task<ActionResult<MacroResource>> GetMacro(Guid id)
    {
        logger.LogInformation($"Fetching Macro with ID: {id}");
        var macro = await mediator.Send(new Application.Queries.Settings.Macros.GetQuery(id));

        if (macro == null)
        {
            logger.LogWarning($"Macro with ID: {id} not found");
            return NotFound();
        }
        logger.LogInformation($"Macro with ID: {id} retrieved successfully");
        return macro;
    }

    [HttpPost("Macros")]
    public async Task<ActionResult<MacroResource>> PostMacro([FromBody] MacroResource macroResource)
    {
        logger.LogInformation("Creating new Macro");
        var result = await mediator.Send(new Application.Commands.Settings.Macros.PostCommand(macroResource));
        logger.LogInformation("Macro created successfully");
        return result;
    }

    [HttpPut("Macros")]
    public async Task<ActionResult<MacroResource>> PutMacro([FromBody] MacroResource macroResource)
    {
        logger.LogInformation("Updating Macro");
        var result = await mediator.Send(new Application.Commands.Settings.Macros.PutCommand(macroResource));
        logger.LogInformation("Macro updated successfully");
        return result;
    }

    #endregion Macros

    #region Vat

    [HttpDelete("Vat/{id}")]
    public async Task<ActionResult<Vat>> DeleteVAT(Guid id)
    {
        logger.LogInformation($"Deleting VAT with ID: {id}");
        var vat = await mediator.Send(new Application.Commands.Settings.Vats.DeleteCommand(id));
        if (vat == null)
        {
            logger.LogWarning($"VAT with ID: {id} not found");
            return NotFound();
        }
        logger.LogInformation($"VAT with ID: {id} deleted successfully");
        return vat;
    }

    [HttpGet("Vat")]
    public async Task<IEnumerable<Vat>> GetVAT()
    {
        logger.LogInformation("Fetching VAT list");
        var result = await mediator.Send(new Application.Queries.Settings.Vats.ListQuery());
        logger.LogInformation($"Retrieved {result.Count()} VATs");
        return result;
    }

    [HttpGet("Vat/{id}")]
    public async Task<ActionResult<Vat>> GetVAT(Guid id)
    {
        logger.LogInformation($"Fetching VAT with ID: {id}");
        var vat = await mediator.Send(new Application.Queries.Settings.Vats.GetQuery(id));

        if (vat == null)
        {
            logger.LogWarning($"VAT with ID: {id} not found");
            return NotFound();
        }
        logger.LogInformation($"VAT with ID: {id} retrieved successfully");
        return vat;
    }

    [HttpPost("Vat")]
    public async Task<ActionResult<Vat>> PostVAT([FromBody] Vat vat)
    {
        logger.LogInformation("Creating new VAT");
        var result = await mediator.Send(new Application.Commands.Settings.Vats.PostCommand(vat));
        logger.LogInformation("VAT created successfully");
        return result;
    }

    [HttpPut("Vat")]
    public async Task<ActionResult<Vat>> PutVAT([FromBody] Vat vat)
    {
        logger.LogInformation("Updating VAT");
        var result = await mediator.Send(new Application.Commands.Settings.Vats.PutCommand(vat));
        logger.LogInformation("VAT updated successfully");
        return result;
    }

    #endregion Vat

    #region CalendarRule
        
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

    #endregion CalendarRule
}
