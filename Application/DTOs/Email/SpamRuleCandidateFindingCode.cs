// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

namespace Klacks.Api.Application.DTOs.Email;

public enum SpamRuleCandidateFindingCode
{
    NoJunkFolder = 0,
    JunkFolderEmpty = 1,
    TooFewJunkMails = 2,
    NoRecurringDomain = 3,
    CoveredDomainsLeftOut = 4,
    InactiveRuleExists = 5,
    OwnDomainWithheld = 6,
    KnownContactDomainWithheld = 7,
    DomainAlsoInInbox = 8,
    ParentDomainRuleDoesNotCoverSubdomain = 9,
    UnusableSenders = 10,
    SenderListTruncated = 11,
    CandidateListTruncated = 12,
    OtherRuleTypesNotEvaluated = 13,
    BelowMinimumLeftOut = 14
}
