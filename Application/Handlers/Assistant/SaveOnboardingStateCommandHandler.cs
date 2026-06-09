// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Persists a Klacksy setup-tour state change via the onboarding service and returns the
/// recomputed onboarding slice for the frontend.
/// </summary>

using Klacks.Api.Application.Commands.Assistant;
using Klacks.Api.Application.DTOs.Assistant;
using Klacks.Api.Application.Interfaces.Assistant;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Handlers.Assistant;

public class SaveOnboardingStateCommandHandler : IRequestHandler<SaveOnboardingStateCommand, OnboardingResource?>
{
    private readonly IOnboardingService _onboardingService;

    public SaveOnboardingStateCommandHandler(IOnboardingService onboardingService)
    {
        _onboardingService = onboardingService;
    }

    public async Task<OnboardingResource?> Handle(SaveOnboardingStateCommand request, CancellationToken cancellationToken)
    {
        return await _onboardingService.UpdateStateAsync(
            request.Status,
            request.CompletedStation,
            request.UserRights,
            cancellationToken);
    }
}
