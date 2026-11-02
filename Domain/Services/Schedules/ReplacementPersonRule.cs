// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

using Klacks.Api.Domain.Enums;
using Klacks.Api.Domain.Models.Staffs;

namespace Klacks.Api.Domain.Services.Schedules;

/// <summary>
/// Who can take over a replacement: an employee or an external employee, never a customer. Single definition for the
/// write guard that refuses a replacement for any other client and for the candidate pools that must offer only
/// people the guard accepts.
/// </summary>
public static class ReplacementPersonRule
{
    /// <summary>True for an employee or an external employee.</summary>
    /// <param name="type">Type of the client</param>
    public static bool IsEligible(EntityTypeEnum type) => type is EntityTypeEnum.Employee or EntityTypeEnum.ExternEmp;

    /// <summary>The clients of the sequence that can take over a replacement, in their order.</summary>
    /// <param name="clients">Candidate clients of any type</param>
    public static IEnumerable<Client> EligibleOnly(IEnumerable<Client> clients) => clients.Where(client => IsEligible(client.Type));
}
