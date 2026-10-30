// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

using Klacks.Api.Domain.Models.Email;

namespace Klacks.Api.Application.DTOs.Email;

/// <summary>
/// Everything the spam rule candidate advisor needs, gathered by the skill: the junk mails folded into domains, all spam
/// rules (also the switched-off ones), the domains of the own mailbox and of known contacts, and the mails the same
/// domains have in the inbox and client folders.
/// </summary>
/// <param name="JunkFolderConfigured">False when no junk folder is configured</param>
/// <param name="JunkMailCount">Number of mails in the junk folder</param>
/// <param name="SenderListTruncated">True when the junk folder has more distinct sender addresses than were read</param>
/// <param name="Grouping">The junk mails folded into sender domains</param>
/// <param name="Rules">All spam rules, active and inactive</param>
/// <param name="OwnDomains">Lower-case domains of the mailbox the installation sends from and reads</param>
/// <param name="ContactDomains">Lower-case domains of the e-mail addresses of known contacts</param>
/// <param name="InboxMailsByDomain">Lower-case domain to number of mails it has in the inbox and client folders</param>
public sealed record SpamRuleCandidateFacts(
    bool JunkFolderConfigured,
    int JunkMailCount,
    bool SenderListTruncated,
    SenderDomainGrouping Grouping,
    IReadOnlyList<SpamRule> Rules,
    IReadOnlySet<string> OwnDomains,
    IReadOnlySet<string> ContactDomains,
    IReadOnlyDictionary<string, int> InboxMailsByDomain);
