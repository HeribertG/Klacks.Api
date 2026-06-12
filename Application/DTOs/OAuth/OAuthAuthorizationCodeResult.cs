// Copyright (c) Heribert Gasparoli Private. All rights reserved.

namespace Klacks.Api.Application.DTOs.OAuth;

public record OAuthAuthorizationCodeResult(
    string? Code,
    string? Error,
    string? ErrorDescription)
{
    public static OAuthAuthorizationCodeResult Success(string code) => new(code, null, null);

    public static OAuthAuthorizationCodeResult Rejected(string error, string description) => new(null, error, description);
}
