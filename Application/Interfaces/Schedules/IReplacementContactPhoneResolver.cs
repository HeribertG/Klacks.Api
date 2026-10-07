// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Resolves the number a planner should call to reach replacement candidates (mobile before fixed line). Group
/// visibility is applied here, not by the caller: an employee the calling user may not see gets no number, the
/// same as one without any number, so a phone list can never reveal a hidden employee.
/// </summary>

namespace Klacks.Api.Application.Interfaces.Schedules;

public interface IReplacementContactPhoneResolver
{
    /// <summary>
    /// Phone numbers of the visible employees among the given ids, read with one query; employees without a
    /// number or outside the caller's visibility are missing from the result.
    /// </summary>
    /// <param name="clientIds">Candidate employees</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task<IReadOnlyDictionary<Guid, string>> ResolveAsync(
        IReadOnlyCollection<Guid> clientIds, CancellationToken cancellationToken = default);
}
