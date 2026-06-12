// Copyright (c) Heribert Gasparoli Private. All rights reserved.

namespace Klacks.Api.Application.DTOs.OAuth;

public record OAuthClientRegistrationResult(
    OAuthClientRegistrationResponse? Response,
    OAuthErrorResponse? Error)
{
    public static OAuthClientRegistrationResult Success(OAuthClientRegistrationResponse response) => new(response, null);

    public static OAuthClientRegistrationResult Rejected(string error, string description) =>
        new(null, new OAuthErrorResponse(error, description));
}
