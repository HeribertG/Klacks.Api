// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// The single place that decides whether a spam rule matches a received email, shared by the spam filter and by the
/// advice on spam rule candidates so both always agree. SenderDomain compares the whole domain of the sender address
/// for equality (ignoring case), so a rule for example.com does NOT match mail from mail.example.com; SenderContains
/// looks for the pattern inside the address or the display name. The sender domain is read from the part after the last
/// at-sign; a display-name form such as "Name &lt;a@b.com&gt;" is reduced to the address, and a trailing dot is dropped.
/// </summary>
/// <param name="email">The received email whose sender, subject and body are checked</param>
/// <param name="rule">The spam rule with its type and pattern</param>
/// <param name="fromAddress">The sender address as stored on the email</param>

using Klacks.Api.Domain.Enums;
using Klacks.Api.Domain.Models.Email;

namespace Klacks.Api.Domain.Services.Email;

public static class SpamRuleMatcher
{
    private const char AddressOpen = '<';
    private const char AddressClose = '>';
    private const char AtSign = '@';
    private const char DomainRootDot = '.';

    public static bool Matches(ReceivedEmail email, SpamRule rule)
    {
        return rule.RuleType switch
        {
            SpamRuleType.SenderContains =>
                ContainsIgnoreCase(email.FromAddress, rule.Pattern) ||
                ContainsIgnoreCase(email.FromName, rule.Pattern),

            SpamRuleType.SenderDomain =>
                MatchesSenderDomain(email.FromAddress, rule.Pattern),

            SpamRuleType.SubjectContains =>
                ContainsIgnoreCase(email.Subject, rule.Pattern),

            SpamRuleType.BodyContains =>
                ContainsIgnoreCase(email.BodyText, rule.Pattern) ||
                ContainsIgnoreCase(email.BodyHtml, rule.Pattern),

            _ => false
        };
    }

    public static bool MatchesSenderDomain(string? fromAddress, string pattern)
    {
        if (string.IsNullOrEmpty(pattern))
        {
            return false;
        }

        var domain = ExtractDomain(fromAddress);
        return domain != null && string.Equals(domain, pattern, StringComparison.OrdinalIgnoreCase);
    }

    public static string? ExtractDomain(string? fromAddress)
    {
        if (string.IsNullOrWhiteSpace(fromAddress))
        {
            return null;
        }

        var address = fromAddress.Trim();
        var open = address.LastIndexOf(AddressOpen);
        if (open >= 0)
        {
            var close = address.IndexOf(AddressClose, open);
            address = close > open ? address[(open + 1)..close] : address[(open + 1)..];
        }

        var at = address.LastIndexOf(AtSign);
        if (at < 0)
        {
            return null;
        }

        var domain = address[(at + 1)..].Trim().TrimEnd(DomainRootDot);
        return domain.Length == 0 ? null : domain;
    }

    private static bool ContainsIgnoreCase(string? text, string pattern)
    {
        if (string.IsNullOrEmpty(text) || string.IsNullOrEmpty(pattern))
        {
            return false;
        }

        return text.Contains(pattern, StringComparison.OrdinalIgnoreCase);
    }
}
