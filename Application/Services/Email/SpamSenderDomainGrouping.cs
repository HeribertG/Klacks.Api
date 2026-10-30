// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Folds sender addresses into sender domains with the same domain extraction the spam filter uses
/// (SpamRuleMatcher.ExtractDomain). A domain is kept only when it is a plausible host name (ASCII letters, digits and
/// hyphens in dot-separated labels, length-capped), because it comes from the sender of the mail and ends up in a sentence
/// the model reads; anything else is counted as unusable. That includes Unicode spellings: mail transports the punycode
/// form, the matcher does not convert between the two, and a Unicode lookalike must not reach the model as a domain. The
/// check runs before lower-casing, which folds some non-ASCII letters to ASCII.
/// </summary>
/// <param name="senders">Sender addresses with their mail counts</param>
/// <param name="domain">A candidate host name</param>

using Klacks.Api.Application.Constants;
using Klacks.Api.Application.DTOs.Email;
using Klacks.Api.Domain.Models.Email;
using Klacks.Api.Domain.Services.Email;

namespace Klacks.Api.Application.Services.Email;

public static class SpamSenderDomainGrouping
{
    private const char LabelSeparator = '.';
    private const char Hyphen = '-';

    public static SenderDomainGrouping Group(IEnumerable<SenderMailCount> senders)
    {
        var byDomain = new Dictionary<string, List<SenderMailCount>>(StringComparer.Ordinal);
        var unusable = 0;

        foreach (var sender in senders)
        {
            var extracted = SpamRuleMatcher.ExtractDomain(sender.FromAddress);
            if (extracted == null || !IsPlausibleHostname(extracted))
            {
                unusable += sender.MailCount;
                continue;
            }

            var domain = extracted.ToLowerInvariant();
            if (!byDomain.TryGetValue(domain, out var list))
            {
                list = [];
                byDomain[domain] = list;
            }

            list.Add(sender);
        }

        var groups = byDomain
            .Select(entry => new SenderDomainGroup(entry.Key, entry.Value.Sum(sender => sender.MailCount), entry.Value))
            .OrderByDescending(group => group.MailCount)
            .ThenBy(group => group.Domain, StringComparer.Ordinal)
            .ToList();

        return new SenderDomainGrouping(groups, unusable);
    }

    public static bool IsPlausibleHostname(string domain)
    {
        if (domain.Length == 0 || domain.Length > SpamRuleCandidateLimits.MaxHostnameLength)
        {
            return false;
        }

        var labels = domain.Split(LabelSeparator);
        return labels.Length > 1 && labels.All(IsPlausibleLabel);
    }

    private static bool IsPlausibleLabel(string label)
    {
        if (label.Length == 0
            || label.Length > SpamRuleCandidateLimits.MaxHostnameLabelLength
            || label[0] == Hyphen
            || label[^1] == Hyphen)
        {
            return false;
        }

        return label.All(character => char.IsAsciiLetterOrDigit(character) || character == Hyphen);
    }
}
