// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

namespace Klacks.Api.Domain.Enums;

public enum OAuthTrustOutcome
{
    Trusted = 0,
    NotTrusted = 1,
    LocalProgramsDisabled = 2,
    ConnectionsDisabled = 3
}
