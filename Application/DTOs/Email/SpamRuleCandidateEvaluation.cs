// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

namespace Klacks.Api.Application.DTOs.Email;

/// <summary>
/// Outcome of looking for sender domains that keep landing in the junk folder without a rule. The facts (counts, the
/// listed domains, the limits applied) are kept apart from the findings and the recommendation, which are computed
/// deterministically; nothing is changed and no mail subject, body or display name is part of it.
/// </summary>
/// <param name="JunkMailCount">Number of mails in the junk folder</param>
/// <param name="MinimumJunkMails">Mails one domain needs in the junk folder to count as recurring</param>
/// <param name="DomainsWithEnoughMails">Number of domains that reached the minimum</param>
/// <param name="CoveredDomainCount">Of those, domains an active rule already catches (left out)</param>
/// <param name="WithheldDomainCount">Of those, domains that are not proposed for another reason</param>
/// <param name="CandidateCount">Number of domains that qualify for a new rule, before the listing limit</param>
/// <param name="Candidates">The qualifying domains, most junk mails first, at most MaxCandidatesListed</param>
/// <param name="Withheld">Domains not proposed, with the reason, at most MaxWithheldListed</param>
/// <param name="UnusableMailCount">Junk mails whose sender has no usable domain</param>
/// <param name="MaxCandidatesListed">The listing limit applied to Candidates</param>
/// <param name="SenderListTruncated">True when the junk folder has more distinct sender addresses than were read</param>
/// <param name="Findings">Deterministic findings, most important first</param>
/// <param name="Recommendation">Conservative summary, or an honest statement that there is no recommendation</param>
public sealed record SpamRuleCandidateEvaluation(
    int JunkMailCount,
    int MinimumJunkMails,
    int DomainsWithEnoughMails,
    int CoveredDomainCount,
    int WithheldDomainCount,
    int CandidateCount,
    IReadOnlyList<SpamRuleCandidate> Candidates,
    IReadOnlyList<SpamRuleWithheldDomain> Withheld,
    int UnusableMailCount,
    int MaxCandidatesListed,
    bool SenderListTruncated,
    IReadOnlyList<SpamRuleCandidateFinding> Findings,
    string Recommendation);
