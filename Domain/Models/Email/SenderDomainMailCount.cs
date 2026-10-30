// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

namespace Klacks.Api.Domain.Models.Email;

/// <summary>
/// Number of received emails whose sender address ends in one domain.
/// </summary>
/// <param name="Domain">Lower-case domain taken after the at-sign of the stored sender address</param>
/// <param name="MailCount">Number of emails from that domain</param>
public sealed record SenderDomainMailCount(string Domain, int MailCount);
