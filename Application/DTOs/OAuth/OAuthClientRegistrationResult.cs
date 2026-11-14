// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

using Microsoft.AspNetCore.Http;

namespace Klacks.Api.Application.DTOs.OAuth;

public record OAuthClientRegistrationResult(
    OAuthClientRegistrationResponse? Response,
    OAuthErrorResponse? Error,
    int ErrorStatusCode = StatusCodes.Status400BadRequest)
{
    public static OAuthClientRegistrationResult Success(OAuthClientRegistrationResponse response) => new(response, null);

    public static OAuthClientRegistrationResult Rejected(string error, string description) =>
        new(null, new OAuthErrorResponse(error, description));

    public static OAuthClientRegistrationResult Forbidden(string error, string description) =>
        new(null, new OAuthErrorResponse(error, description), StatusCodes.Status403Forbidden);
}
