// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Registration form of the anonymous first start: the own admin account plus the one-time setup code the installer
/// printed. The code is only checked for requests that do not come straight from the machine itself.
/// </summary>
/// <param name="SetupCode">One-time setup code from the installer (KLACKS_SETUP_CODE in .env)</param>

namespace Klacks.Api.Application.DTOs.Registrations;

public class FirstStartRegistrationResource : RegistrationResource
{
    public string SetupCode { get; set; } = string.Empty;
}
