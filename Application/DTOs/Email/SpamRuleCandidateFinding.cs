// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

namespace Klacks.Api.Application.DTOs.Email;

/// <summary>
/// One deterministic finding of the spam rule candidate advisor: a code the tests pin and a server-built sentence
/// filled only with counts and host names, never with a subject, a body or a display name.
/// </summary>
/// <param name="Code">Finding kind</param>
/// <param name="Statement">Factual sentence about the junk senders</param>
public sealed record SpamRuleCandidateFinding(SpamRuleCandidateFindingCode Code, string Statement);
