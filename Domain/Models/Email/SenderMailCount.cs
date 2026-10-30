// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

namespace Klacks.Api.Domain.Models.Email;

/// <summary>
/// Number of received emails of one folder that carry one exact sender address.
/// </summary>
/// <param name="FromAddress">The sender address as stored on the emails</param>
/// <param name="MailCount">Number of emails with that sender address</param>
public sealed record SenderMailCount(string FromAddress, int MailCount);
