using Klacks.Api.Application.Commands.Settings.Settings;
using Klacks.Api.Infrastructure.Email;
using Klacks.Api.Presentation.DTOs.Settings;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Klacks.Api.Presentation.Controllers.v1.UserBackend;

public class GeneralSettingsController : BaseController
{
    private readonly IMediator mediator;
    private readonly IEmailTestService emailTestService;

    public GeneralSettingsController(IMediator mediator, ILogger<GeneralSettingsController> logger, IEmailTestService emailTestService)
    {
        this.mediator = mediator;
        this.emailTestService = emailTestService;
    }

    [HttpPost("AddSetting")]
    public async Task<Klacks.Api.Domain.Models.Settings.Settings> AddSetting([FromBody] Klacks.Api.Domain.Models.Settings.Settings setting)
    {
        var res = await mediator.Send(new PostCommand(setting));
        return res;
    }

    [HttpGet("GetSetting/{type}")]
    public async Task<ActionResult<Klacks.Api.Domain.Models.Settings.Settings?>> GetSetting(string type)
    {
        var setting = await mediator.Send(new Application.Queries.Settings.Settings.GetQuery(type));
        if (setting == null)
        {
            return NotFound();
        }

        return setting;
    }

    [HttpGet("GetSettingsList")]
    public async Task<IEnumerable<Klacks.Api.Domain.Models.Settings.Settings>> GetSettingsListAsync()
    {
        var settings = await mediator.Send(new Application.Queries.Settings.Settings.ListQuery());
        return settings;
    }

    [HttpPut("PutSetting")]
    public async Task<Klacks.Api.Domain.Models.Settings.Settings> PutSetting([FromBody] Klacks.Api.Domain.Models.Settings.Settings setting)
    {
        var res = await mediator.Send(new PutCommand(setting));
        return res;
    }

    [HttpPost("TestEmailConfiguration")]
    public async Task<ActionResult<EmailTestResult>> TestEmailConfiguration([FromBody] EmailTestRequest request)
    {
        try
        {
            var result = await emailTestService.TestConnectionAsync(request);
            return Ok(result);
        }
        catch (Exception ex)
        {
            return Ok(new EmailTestResult
            {
                Success = false,
                Message = "An unexpected error occurred during the test.",
                ErrorDetails = ex.Message
            });
        }
    }
}
