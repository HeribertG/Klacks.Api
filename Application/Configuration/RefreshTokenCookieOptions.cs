// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Configurable attributes of the refresh token cookie, bound from "Auth:RefreshCookie".
/// </summary>
/// <param name="SameSite">SameSite mode of the cookie. Strict fits every same-origin deployment (nginx, native, Playground);
/// a development setup with the Ui on another origin than the Api needs None.</param>

using Microsoft.AspNetCore.Http;

namespace Klacks.Api.Application.Configuration;

public class RefreshTokenCookieOptions
{
    public const string SectionName = "Auth:RefreshCookie";

    public SameSiteMode SameSite { get; set; } = SameSiteMode.Strict;
}
