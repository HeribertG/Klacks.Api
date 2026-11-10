// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Replaces the seeded admin with the operator's own admin account.
/// </summary>
/// <param name="NewAdmin">Registration form of the new admin</param>
/// <param name="IsFirstStart">True for the anonymous first-start path, which is only allowed while
/// IAdminSetupGateService.IsFirstStartSetupOpenAsync holds; false for the seeded admin signed in</param>
/// <param name="IsLocalRequest">The first-start request comes straight from the machine itself, so no setup code is needed</param>
/// <param name="SetupCode">One-time setup code typed on the first-start page</param>

using Klacks.Api.Application.DTOs.Registrations;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Commands.Accounts;

public record CompleteOwnAdminSetupCommand(
    RegistrationResource NewAdmin,
    bool IsFirstStart = false,
    bool IsLocalRequest = false,
    string? SetupCode = null) : IRequest<bool>;
