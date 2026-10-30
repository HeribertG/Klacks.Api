// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Deterministic advice on which sender domains keep landing in the junk folder without a spam rule. A domain qualifies
/// when it reaches SpamRuleCandidateLimits.MinimumJunkMailsPerDomain junk mails and no active rule catches all of its
/// sender addresses; the check runs through SpamRuleMatcher, the same code the spam filter uses, so a domain a rule would
/// already hit is never proposed. A rule that exists but is switched off, the own mailbox domain and domains of known
/// contacts are withheld instead of proposed. With too little data the answer is an honest "no recommendation". Pure and
/// static, so every branch is pinned by a unit test; the model only relays it.
/// </summary>
/// <param name="facts">The gathered junk senders, rules and protected domains</param>

using Klacks.Api.Application.Constants;
using Klacks.Api.Application.DTOs.Email;
using Klacks.Api.Domain.Enums;
using Klacks.Api.Domain.Models.Email;
using Klacks.Api.Domain.Services.Email;

namespace Klacks.Api.Application.Services.Email;

public static class SpamRuleCandidateAdvisor
{
    private const string ParentDomainSeparator = ".";
    private const string ConfirmationNotice =
        "Nothing has been changed. A rule is created in a separate step the system confirms once (rule type SenderDomain, the domain as pattern); "
        + "it matches that exact domain only, not its subdomains, and afterwards re-checks the inbox and client folders and moves matching mail to Junk.";

    public static SpamRuleCandidateEvaluation Evaluate(SpamRuleCandidateFacts facts)
    {
        var minimum = SpamRuleCandidateLimits.MinimumJunkMailsPerDomain;

        if (!facts.JunkFolderConfigured)
        {
            return WithoutData(facts, SpamRuleCandidateFindingCode.NoJunkFolder,
                "No junk folder is configured, so there are no junk mails to look at.",
                "No recommendation: no junk folder is configured.");
        }

        if (facts.JunkMailCount == 0)
        {
            return WithoutData(facts, SpamRuleCandidateFindingCode.JunkFolderEmpty,
                "The junk folder holds no mail.",
                "No recommendation: the junk folder holds no mail.");
        }

        if (facts.JunkMailCount < minimum)
        {
            return WithoutData(facts, SpamRuleCandidateFindingCode.TooFewJunkMails,
                $"The junk folder holds only {facts.JunkMailCount} mail(s); at least {minimum} from one domain are needed to call a sender recurring.",
                $"No recommendation: the junk folder holds only {facts.JunkMailCount} mail(s), too few to tell a recurring sender from a one-off.");
        }

        var enough = facts.Grouping.Groups.Where(group => group.MailCount >= minimum).ToList();
        var belowMinimum = facts.Grouping.Groups.Count - enough.Count;
        var (candidates, withheld, coveredCount) = Classify(facts, enough);

        var listed = candidates.Take(SpamRuleCandidateLimits.MaxCandidatesListed).ToList();
        var findings = BuildFindings(facts, listed, candidates.Count, enough.Count, withheld, coveredCount, belowMinimum);

        return new SpamRuleCandidateEvaluation(
            facts.JunkMailCount,
            minimum,
            enough.Count,
            coveredCount,
            withheld.Count,
            candidates.Count,
            listed,
            withheld.Take(SpamRuleCandidateLimits.MaxWithheldListed).ToList(),
            facts.Grouping.UnusableMailCount,
            SpamRuleCandidateLimits.MaxCandidatesListed,
            facts.SenderListTruncated,
            findings,
            BuildRecommendation(facts, listed, candidates.Count, enough.Count, withheld.Count, coveredCount, belowMinimum));
    }

    private static (List<SpamRuleCandidate> Candidates, List<SpamRuleWithheldDomain> Withheld, int CoveredCount) Classify(
        SpamRuleCandidateFacts facts, IReadOnlyList<SenderDomainGroup> enough)
    {
        var activeRules = facts.Rules.Where(rule => rule.IsActive).ToList();
        var inactiveRules = facts.Rules.Where(rule => !rule.IsActive).ToList();
        var candidates = new List<SpamRuleCandidate>();
        var withheld = new List<SpamRuleWithheldDomain>();
        var covered = 0;

        foreach (var group in enough)
        {
            if (CoversAllSenders(activeRules, group))
            {
                covered++;
            }
            else if (CoversAllSenders(inactiveRules, group))
            {
                withheld.Add(new SpamRuleWithheldDomain(group.Domain, group.MailCount, SpamRuleWithheldReason.InactiveRuleExists));
            }
            else if (facts.OwnDomains.Contains(group.Domain))
            {
                withheld.Add(new SpamRuleWithheldDomain(group.Domain, group.MailCount, SpamRuleWithheldReason.OwnDomain));
            }
            else if (facts.ContactDomains.Contains(group.Domain))
            {
                withheld.Add(new SpamRuleWithheldDomain(group.Domain, group.MailCount, SpamRuleWithheldReason.KnownContactDomain));
            }
            else
            {
                candidates.Add(new SpamRuleCandidate(
                    group.Domain,
                    group.MailCount,
                    group.SenderCount,
                    facts.InboxMailsByDomain.GetValueOrDefault(group.Domain),
                    HasParentDomainRule(facts.Rules, group.Domain)));
            }
        }

        return (candidates, withheld, covered);
    }

    private static bool CoversAllSenders(IReadOnlyList<SpamRule> rules, SenderDomainGroup group)
    {
        return rules.Any(rule => group.Senders.All(sender =>
            SpamRuleMatcher.Matches(new ReceivedEmail { FromAddress = sender.FromAddress }, rule)));
    }

    private static bool HasParentDomainRule(IReadOnlyList<SpamRule> rules, string domain)
    {
        return rules.Any(rule =>
            rule.RuleType == SpamRuleType.SenderDomain
            && !string.IsNullOrWhiteSpace(rule.Pattern)
            && domain.EndsWith(ParentDomainSeparator + rule.Pattern.Trim(), StringComparison.OrdinalIgnoreCase));
    }

    private static List<SpamRuleCandidateFinding> BuildFindings(
        SpamRuleCandidateFacts facts,
        IReadOnlyList<SpamRuleCandidate> listed,
        int candidateCount,
        int enoughCount,
        IReadOnlyList<SpamRuleWithheldDomain> withheld,
        int coveredCount,
        int belowMinimum)
    {
        var minimum = SpamRuleCandidateLimits.MinimumJunkMailsPerDomain;
        var findings = new List<SpamRuleCandidateFinding>();

        if (enoughCount == 0)
        {
            findings.Add(new SpamRuleCandidateFinding(SpamRuleCandidateFindingCode.NoRecurringDomain,
                $"No sender domain has {minimum} or more mails in the junk folder."));
        }

        if (coveredCount > 0)
        {
            findings.Add(new SpamRuleCandidateFinding(SpamRuleCandidateFindingCode.CoveredDomainsLeftOut,
                $"{coveredCount} domain(s) with at least {minimum} junk mails are already caught by an active rule and are left out."));
        }

        AddWithheldFinding(findings, withheld, SpamRuleWithheldReason.InactiveRuleExists, SpamRuleCandidateFindingCode.InactiveRuleExists,
            "domain(s) have a rule that is switched off; switching that rule on is the alternative to a new one");
        AddWithheldFinding(findings, withheld, SpamRuleWithheldReason.OwnDomain, SpamRuleCandidateFindingCode.OwnDomainWithheld,
            "domain(s) belong to the own mailbox and are not proposed");
        AddWithheldFinding(findings, withheld, SpamRuleWithheldReason.KnownContactDomain, SpamRuleCandidateFindingCode.KnownContactDomainWithheld,
            "domain(s) are used by known contacts and are not proposed, because a rule would also catch their mail");

        var withInbox = listed.Count(candidate => candidate.InboxMailCount > 0);
        if (withInbox > 0)
        {
            findings.Add(new SpamRuleCandidateFinding(SpamRuleCandidateFindingCode.DomainAlsoInInbox,
                $"{withInbox} listed domain(s) also have mail in the inbox or client folders; a rule would move that mail to Junk, too."));
        }

        var withParentRule = listed.Count(candidate => candidate.HasParentDomainRule);
        if (withParentRule > 0)
        {
            findings.Add(new SpamRuleCandidateFinding(SpamRuleCandidateFindingCode.ParentDomainRuleDoesNotCoverSubdomain,
                $"{withParentRule} listed domain(s) are subdomains of a domain that has a sender-domain rule; that rule matches the exact domain only and does not catch them."));
        }

        if (belowMinimum > 0)
        {
            findings.Add(new SpamRuleCandidateFinding(SpamRuleCandidateFindingCode.BelowMinimumLeftOut,
                $"{belowMinimum} domain(s) have fewer than {minimum} junk mails and are left out as one-offs."));
        }

        AddLimitFindings(findings, facts, candidateCount, listed.Count);
        return findings;
    }

    private static void AddLimitFindings(
        List<SpamRuleCandidateFinding> findings, SpamRuleCandidateFacts facts, int candidateCount, int listedCount)
    {
        if (facts.Grouping.UnusableMailCount > 0)
        {
            findings.Add(new SpamRuleCandidateFinding(SpamRuleCandidateFindingCode.UnusableSenders,
                $"{facts.Grouping.UnusableMailCount} junk mail(s) have no usable sender domain and were left out."));
        }

        if (facts.SenderListTruncated)
        {
            findings.Add(new SpamRuleCandidateFinding(SpamRuleCandidateFindingCode.SenderListTruncated,
                $"Only the {SpamRuleCandidateLimits.MaxJunkSenderRows} most frequent sender addresses were read; less frequent domains may be missing."));
        }

        if (candidateCount > listedCount)
        {
            findings.Add(new SpamRuleCandidateFinding(SpamRuleCandidateFindingCode.CandidateListTruncated,
                $"{candidateCount} domains qualify; the {listedCount} with the most junk mails are listed."));
        }

        if (candidateCount > 0)
        {
            findings.Add(new SpamRuleCandidateFinding(SpamRuleCandidateFindingCode.OtherRuleTypesNotEvaluated,
                "Rules on the subject, the body text or the sender's display name are not evaluated here; a listed domain may already be caught by one of them."));
        }
    }

    private static void AddWithheldFinding(
        List<SpamRuleCandidateFinding> findings,
        IReadOnlyList<SpamRuleWithheldDomain> withheld,
        SpamRuleWithheldReason reason,
        SpamRuleCandidateFindingCode code,
        string text)
    {
        var count = withheld.Count(entry => entry.Reason == reason);
        if (count > 0)
        {
            findings.Add(new SpamRuleCandidateFinding(code, $"{count} {text}."));
        }
    }

    private static string BuildRecommendation(
        SpamRuleCandidateFacts facts,
        IReadOnlyList<SpamRuleCandidate> listed,
        int candidateCount,
        int enoughCount,
        int withheldCount,
        int coveredCount,
        int belowMinimum)
    {
        var minimum = SpamRuleCandidateLimits.MinimumJunkMailsPerDomain;

        if (enoughCount == 0)
        {
            return $"No recommendation: no sender domain has {minimum} or more mails in the junk folder.{TruncationNote(facts)}";
        }

        if (candidateCount == 0)
        {
            return $"No recommendation: every sender domain with at least {minimum} junk mails is already covered by a rule ({coveredCount}) or withheld ({withheldCount}); {belowMinimum} more are below the minimum.{TruncationNote(facts)}";
        }

        var top = listed[0];
        var withInbox = listed.Count(candidate => candidate.InboxMailCount > 0);
        var inboxNote = withInbox > 0
            ? $" {withInbox} of the listed domain(s) also have mail in the inbox or client folders that a rule would move to Junk."
            : string.Empty;

        return $"{candidateCount} sender domain(s) landed at least {minimum} times in the junk folder without a rule; the most frequent is {top.Domain} with {top.JunkMailCount} junk mails."
            + $"{inboxNote}{TruncationNote(facts)} {ConfirmationNotice}";
    }

    private static string TruncationNote(SpamRuleCandidateFacts facts)
    {
        return facts.SenderListTruncated
            ? $" Only the {SpamRuleCandidateLimits.MaxJunkSenderRows} most frequent sender addresses were read, so less frequent domains may be missing."
            : string.Empty;
    }

    private static SpamRuleCandidateEvaluation WithoutData(
        SpamRuleCandidateFacts facts, SpamRuleCandidateFindingCode code, string statement, string recommendation)
    {
        return new SpamRuleCandidateEvaluation(
            facts.JunkMailCount,
            SpamRuleCandidateLimits.MinimumJunkMailsPerDomain,
            0,
            0,
            0,
            0,
            [],
            [],
            facts.Grouping.UnusableMailCount,
            SpamRuleCandidateLimits.MaxCandidatesListed,
            facts.SenderListTruncated,
            [new SpamRuleCandidateFinding(code, statement)],
            recommendation);
    }
}
