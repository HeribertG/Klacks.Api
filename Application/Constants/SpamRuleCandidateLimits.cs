// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

namespace Klacks.Api.Application.Constants;

public static class SpamRuleCandidateLimits
{
    public const int MinimumJunkMailsPerDomain = 3;
    public const int MaxJunkSenderRows = 20000;
    public const int MaxInboxLookupDomains = 200;
    public const int MaxCandidatesListed = 20;
    public const int MaxWithheldListed = 20;
    public const int MaxHostnameLength = 253;
    public const int MaxHostnameLabelLength = 63;
}
