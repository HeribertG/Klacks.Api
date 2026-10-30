// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

namespace Klacks.Api.Domain.Models.Email;

/// <summary>
/// Sender addresses of one folder with their mail counts, most frequent first, plus the folder total.
/// </summary>
/// <param name="Senders">Sender addresses ordered by mail count (descending) and address, at most the requested number</param>
/// <param name="TotalMailCount">Number of emails in the folder, counted before the list was cut</param>
/// <param name="IsTruncated">True when more distinct sender addresses exist than Senders holds</param>
public sealed record SenderMailCounts(
    IReadOnlyList<SenderMailCount> Senders,
    int TotalMailCount,
    bool IsTruncated);
