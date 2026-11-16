// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

using Klacks.Api.Domain.Constants;
using Klacks.Api.Domain.Enums;

namespace Klacks.Api.Application.DTOs.OAuth;

public record OAuthAuthorizationCodeResult(
    string? Code,
    string? Error,
    string? ErrorDescription,
    OAuthIssueFailure Failure = OAuthIssueFailure.None)
{
    public bool IsSetupRequired => Failure == OAuthIssueFailure.SetupRequired;

    public static OAuthAuthorizationCodeResult Success(string code) => new(code, null, null);

    public static OAuthAuthorizationCodeResult Rejected(string error, string description, OAuthIssueFailure failure = OAuthIssueFailure.None) =>
        new(null, error, description, failure);

    public static OAuthAuthorizationCodeResult SetupRequired() =>
        new(null, OAuthConstants.ErrorAccessDenied, OAuthConstants.SetupRequiredErrorDescription, OAuthIssueFailure.SetupRequired);
}
