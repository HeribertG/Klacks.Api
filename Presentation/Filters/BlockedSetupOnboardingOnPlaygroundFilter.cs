// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Action filter behind BlockedSetupOnboardingOnPlayground: the setup tour state is installation-wide, and on the
/// Playground every visitor is the shared demo admin, so one visitor would change the tour for all others. A save whose
/// kind resolves to "setup" (also when empty) is answered with HTTP 403 and errorCode PLAYGROUND_FEATURE_BLOCKED before
/// the action runs; every other kind (the per-account intro tour) passes. With the Playground mode off it does nothing.
/// </summary>
/// <param name="mode">Tells whether the instance is the Playground</param>

using Klacks.Api.Application.Commands.Assistant;
using Klacks.Api.Application.Constants;
using Klacks.Api.Domain.Interfaces.Playground;
using Microsoft.AspNetCore.Mvc.Filters;

namespace Klacks.Api.Presentation.Filters;

public class BlockedSetupOnboardingOnPlaygroundFilter : IAsyncActionFilter
{
    private readonly IPlaygroundMode _mode;

    public BlockedSetupOnboardingOnPlaygroundFilter(IPlaygroundMode mode)
    {
        _mode = mode;
    }

    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        if (_mode.IsActive && context.ActionArguments.Values.OfType<SaveOnboardingStateCommand>().Any(IsSetupKind))
        {
            context.Result = PlaygroundProblemResultFactory.FeatureBlocked();
            return;
        }

        await next();
    }

    private static bool IsSetupKind(SaveOnboardingStateCommand command) =>
        OnboardingKind.Resolve(command.Kind) == OnboardingKind.Setup;
}
