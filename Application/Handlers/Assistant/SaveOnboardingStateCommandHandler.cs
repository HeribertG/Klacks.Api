// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Persists a Klacksy tour state change and returns the recomputed slice for the frontend. Kind "intro" goes to the
/// caller's own intro state; kind "setup" (also when empty) goes to the admin-only setup tour and returns null for a
/// non-admin. When the setup tour is completed or dismissed, the same admin's intro is closed so no second tour is
/// offered.
/// </summary>
/// <param name="onboardingService">Installation-wide setup tour state</param>
/// <param name="introTourStateService">Per-account intro tour state</param>

using Klacks.Api.Application.Commands.Assistant;
using Klacks.Api.Application.Constants;
using Klacks.Api.Application.DTOs.Assistant;
using Klacks.Api.Application.Interfaces.Assistant;
using Klacks.Api.Domain.Exceptions;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Handlers.Assistant;

public class SaveOnboardingStateCommandHandler : IRequestHandler<SaveOnboardingStateCommand, OnboardingResource?>
{
    private readonly IOnboardingService _onboardingService;
    private readonly IIntroTourStateService _introTourStateService;

    public SaveOnboardingStateCommandHandler(
        IOnboardingService onboardingService,
        IIntroTourStateService introTourStateService)
    {
        _onboardingService = onboardingService;
        _introTourStateService = introTourStateService;
    }

    public async Task<OnboardingResource?> Handle(SaveOnboardingStateCommand request, CancellationToken cancellationToken)
    {
        var kind = string.IsNullOrWhiteSpace(request.Kind)
            ? OnboardingKind.Setup
            : request.Kind.Trim().ToLowerInvariant();

        if (kind == OnboardingKind.Intro)
        {
            return await _introTourStateService.ApplyAsync(
                request.UserId,
                request.Status,
                request.CompletedStation,
                cancellationToken);
        }

        if (kind != OnboardingKind.Setup)
        {
            throw new InvalidRequestException("Unknown onboarding kind.");
        }

        var result = await _onboardingService.UpdateStateAsync(
            request.Status,
            request.CompletedStation,
            request.UserRights,
            cancellationToken);

        if (result != null && OnboardingStatus.IsResolved(request.Status?.Trim()))
        {
            await _introTourStateService.CloseAfterSetupAsync(request.UserId, cancellationToken);
        }

        return result;
    }
}
