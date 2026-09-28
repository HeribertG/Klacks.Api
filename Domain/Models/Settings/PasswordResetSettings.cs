// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

namespace Klacks.Api.Domain.Models.Settings;

/// <summary>
/// Configuration for password reset functionality.
/// </summary>
/// <param name="BaseUrl">Base URL for the password reset page</param>
public record PasswordResetSettings
{
    public string BaseUrl { get; init; } = "https://localhost:7002";
}
