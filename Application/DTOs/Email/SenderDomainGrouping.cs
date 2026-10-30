// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

namespace Klacks.Api.Application.DTOs.Email;

/// <summary>
/// Sender addresses folded into domains, most frequent domain first, plus the mails whose sender has no usable domain.
/// </summary>
/// <param name="Groups">Domain groups ordered by mail count (descending) and domain</param>
/// <param name="UnusableMailCount">Mails whose sender address has no domain or no plausible host name</param>
public sealed record SenderDomainGrouping(IReadOnlyList<SenderDomainGroup> Groups, int UnusableMailCount);
