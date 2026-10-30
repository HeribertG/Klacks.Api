// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// The rules for ending an employee's membership at an exit date, shared by the skill that records the exit and the
/// advisor that explains what hangs on it: which memberships count as active (relative to the company's today, not to
/// the exit date), the exit date that lies before a membership's start, and the wording of the real options when more
/// than one membership is active. Pure and static so the two can never drift apart.
/// </summary>
/// <param name="memberships">All memberships; the ones of the client are picked here</param>
/// <param name="clientId">The employee whose memberships are meant</param>
/// <param name="today">The company's today</param>

using Klacks.Api.Domain.Constants;
using Klacks.Api.Domain.Models.Associations;

namespace Klacks.Api.Application.Services.Clients;

public static class MembershipExitRules
{
    private const string OpenEndedLabel = "open-ended";

    public static List<Membership> ActiveMemberships(IEnumerable<Membership> memberships, Guid clientId, DateTime today) =>
        memberships
            .Where(m => m.ClientId == clientId)
            .Where(m => m.ValidUntil == null || m.ValidUntil.Value.Date >= today.Date)
            .OrderBy(m => m.ValidFrom)
            .ToList();

    public static bool ExitPrecedesStart(DateTime exitDate, Membership membership) =>
        exitDate < membership.ValidFrom.Date;

    public static string DescribeOptions(IEnumerable<Membership> activeMemberships) =>
        string.Join("; ", activeMemberships.Select(m =>
            $"id={m.Id}, validFrom={m.ValidFrom.ToString(DateTimeFormats.IsoDate)}, " +
            $"validUntil={FormatValidUntil(m.ValidUntil)}"));

    public static string FormatValidUntil(DateTime? validUntil) =>
        validUntil.HasValue ? validUntil.Value.ToString(DateTimeFormats.IsoDate) : OpenEndedLabel;
}
