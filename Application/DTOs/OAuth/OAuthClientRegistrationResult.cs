// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

namespace Klacks.Api.Application.DTOs.OAuth;

public record OAuthClientRegistrationResult(
    OAuthClientRegistrationResponse? Response,
    OAuthErrorResponse? Error)
{
    public static OAuthClientRegistrationResult Success(OAuthClientRegistrationResponse response) => new(response, null);

    public static OAuthClientRegistrationResult Rejected(string error, string description) =>
        new(null, new OAuthErrorResponse(error, description));
}
