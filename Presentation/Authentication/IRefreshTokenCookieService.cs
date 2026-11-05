// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

namespace Klacks.Api.Presentation.Authentication;

public interface IRefreshTokenCookieService
{
    string? Read(HttpRequest request);

    void Write(HttpContext context, string? refreshToken);

    void Delete(HttpContext context);
}
