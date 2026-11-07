// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

using Klacks.Api.Domain.Enums;
using Klacks.Api.Domain.Models.Staffs;
using Klacks.Api.Domain.Services.Clients;

namespace Klacks.Api.Domain.Services.Schedules;

/// <summary>
/// Who can take over a replacement (ClientTypeRules.IsReplacementCandidate: an employee or an external employee, never a
/// customer or a stand-in). Used by the write guard that refuses a replacement for any other client and by the candidate
/// pools that must offer only people the guard accepts.
/// </summary>
public static class ReplacementPersonRule
{
    /// <summary>True for an employee or an external employee.</summary>
    /// <param name="type">Type of the client</param>
    public static bool IsEligible(EntityTypeEnum type) => ClientTypeRules.IsReplacementCandidate(type);

    /// <summary>The clients of the sequence that can take over a replacement, in their order.</summary>
    /// <param name="clients">Candidate clients of any type</param>
    public static IEnumerable<Client> EligibleOnly(IEnumerable<Client> clients) => clients.Where(client => IsEligible(client.Type));
}
