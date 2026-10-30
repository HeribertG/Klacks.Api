// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

using Klacks.Api.Domain.Models.Email;

namespace Klacks.Api.Application.DTOs.Email;

/// <summary>
/// The junk mails of one sender domain: the mail count and the sender addresses behind it, which the rule matcher
/// needs to judge whether an existing rule already covers the whole domain.
/// </summary>
/// <param name="Domain">Lower-case sender domain, a plausible host name</param>
/// <param name="MailCount">Number of junk mails from the domain</param>
/// <param name="Senders">The sender addresses of the domain with their mail counts</param>
public sealed record SenderDomainGroup(string Domain, int MailCount, IReadOnlyList<SenderMailCount> Senders)
{
    public int SenderCount => Senders.Count;
}
