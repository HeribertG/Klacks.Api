// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Application.Commands.Settings.Settings;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Application.DTOs.Settings;
using Klacks.Api.Domain.Common;
using Klacks.Api.Domain.Constants;
using Klacks.Api.Domain.Interfaces.Settings;
using Klacks.Api.Infrastructure.Mediator;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace Klacks.Api.Presentation.Controllers.UserBackend.Settings;

[Authorize(Roles = Roles.Admin)]
public class GeneralSettingsController : BaseController
{
    private readonly IMediator mediator;
    private readonly IEmailTestService emailTestService;
    private readonly ISettingsSecretResolver secretResolver;

    public GeneralSettingsController(
        IMediator mediator,
        ILogger<GeneralSettingsController> logger,
        IEmailTestService emailTestService,
        ISettingsSecretResolver secretResolver)
    {
        this.mediator = mediator;
        this.emailTestService = emailTestService;
        this.secretResolver = secretResolver;
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
        return Ok(setting);
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
    [EnableRateLimiting(Application.Constants.RateLimitingPolicies.ConnectionTest)]
    public async Task<ActionResult<EmailTestResult>> TestEmailConfiguration([FromBody] EmailTestRequest request)
    {
        try
        {
            request.Password = await secretResolver.ResolveBoundAsync(
                Application.Constants.Settings.APP_OUTGOING_SERVER_PASSWORD,
                request.Password,
                new SecretBinding(Application.Constants.Settings.APP_OUTGOING_SERVER, request.Server),
                new SecretBinding(Application.Constants.Settings.APP_OUTGOING_SERVER_USERNAME, request.Username));

            var result = await emailTestService.TestConnectionAsync(request);
            return Ok(result);
        }
        catch (Exception ex)
        {
            return Ok(new EmailTestResult
            {
                Success = false,
                Message = "An unexpected error occurred during the test.",
                MessageKey = "EMAIL_TEST_UNEXPECTED",
                ErrorDetails = ex.Message
            });
        }
    }
}
