// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Resolves the earliest date a client's employment record covers, used by PeriodCapEvaluator to clamp a
/// K6 rolling-average window so it never averages over weeks that predate the client's employment start
/// (see <see cref="Klacks.Api.Domain.Models.Associations.Membership"/>).
/// </summary>

namespace Klacks.Api.Domain.Interfaces.Scheduling;

public interface IClientMembershipStartResolver
{
    Task<DateOnly?> GetValidFromAsync(Guid clientId);
}
