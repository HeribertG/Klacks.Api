// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Microsoft.AspNetCore.Http;

namespace Klacks.Api.Application.DTOs.OAuth;

public record OAuthTokenResult(
    OAuthTokenResponse? Response,
    OAuthErrorResponse? Error,
    int ErrorStatusCode)
{
    public static OAuthTokenResult Success(OAuthTokenResponse response) => new(response, null, 0);

    public static OAuthTokenResult Rejected(string error, string description, int statusCode = StatusCodes.Status400BadRequest) =>
        new(null, new OAuthErrorResponse(error, description), statusCode);
}
