// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Global action filter that blocks the seeded admin account from reaching anything but session
/// self-service and the setup endpoints while its own-admin setup gate is active, so the seeded
/// account cannot be used to operate a deployed instance without ever creating a real admin.
/// </summary>

using System.Security.Claims;
using Klacks.Api.Application.Constants;
using Klacks.Api.Domain.Constants;
using Klacks.Api.Domain.Interfaces.Authentification;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace Klacks.Api.Presentation.Filters;

public class AdminSetupGateFilter : IAsyncActionFilter
{
    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        if (context.ActionDescriptor.EndpointMetadata.OfType<ExemptFromAdminSetupGateAttribute>().Any())
        {
            await next();
            return;
        }

        var userId = context.HttpContext.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (userId != SystemAccounts.SeedAdminUserId)
        {
            await next();
            return;
        }

        var gateService = context.HttpContext.RequestServices.GetRequiredService<IAdminSetupGateService>();
        if (!await gateService.IsGateActiveAsync())
        {
            await next();
            return;
        }

        var problem = new ProblemDetails
        {
            Title = "Forbidden",
            Status = StatusCodes.Status403Forbidden,
            Detail = "The seeded admin account must be replaced with your own admin account before continuing.",
        };
        problem.Extensions["errorCode"] = DeploymentConstants.SetupRequiredErrorCode;

        context.Result = new ObjectResult(problem) { StatusCode = StatusCodes.Status403Forbidden };
    }
}
