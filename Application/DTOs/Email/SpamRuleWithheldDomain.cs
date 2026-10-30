// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

namespace Klacks.Api.Application.DTOs.Email;

/// <summary>
/// A recurring junk sender domain that is deliberately not proposed for a new rule, with the reason.
/// </summary>
/// <param name="Domain">Lower-case sender domain</param>
/// <param name="JunkMailCount">Number of mails from the domain in the junk folder</param>
/// <param name="Reason">Why no new rule is proposed</param>
public sealed record SpamRuleWithheldDomain(string Domain, int JunkMailCount, SpamRuleWithheldReason Reason);
