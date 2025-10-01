using Klacks.Api.Application.Commands.Settings.Settings;
using Klacks.Api.Infrastructure.Email;
using Klacks.Api.Presentation.DTOs.Settings;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Klacks.Api.Presentation.Controllers.v1.UserBackend;

public class GeneralSettingsController : BaseController
{
    private readonly ILogger<GeneralSettingsController> logger;
    private readonly IMediator mediator;
    private readonly IEmailTestService emailTestService;

    public GeneralSettingsController(IMediator mediator, ILogger<GeneralSettingsController> logger, IEmailTestService emailTestService)
    {
        this.mediator = mediator;
        this.logger = logger;
        this.emailTestService = emailTestService;
    }

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
}
