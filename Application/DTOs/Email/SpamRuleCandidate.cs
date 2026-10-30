// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

namespace Klacks.Api.Application.DTOs.Email;

/// <summary>
/// A sender domain that keeps landing in the junk folder and has no rule: facts only, no mail content.
/// </summary>
/// <param name="Domain">Lower-case sender domain, exactly as a SenderDomain rule would have to spell it</param>
/// <param name="JunkMailCount">Number of mails from the domain in the junk folder</param>
/// <param name="SenderCount">Number of distinct sender addresses behind them</param>
/// <param name="InboxMailCount">Mails from the domain in the inbox and client folders that a new rule would move to junk</param>
/// <param name="HasParentDomainRule">True when a SenderDomain rule exists for a parent domain, which does not cover this subdomain</param>
public sealed record SpamRuleCandidate(
    string Domain,
    int JunkMailCount,
    int SenderCount,
    int InboxMailCount,
    bool HasParentDomainRule);
