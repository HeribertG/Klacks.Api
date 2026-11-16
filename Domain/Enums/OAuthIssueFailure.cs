// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

namespace Klacks.Api.Domain.Enums;

public enum OAuthIssueFailure
{
    None = 0,
    InvalidCredentials = 1,
    AccountNotAllowed = 2,
    SetupRequired = 3
}
