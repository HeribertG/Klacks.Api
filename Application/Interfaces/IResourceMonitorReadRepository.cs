// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

using Klacks.Api.Application.DTOs.Dashboard;
using Klacks.Api.Domain.Models.Staffs;

namespace Klacks.Api.Application.Interfaces;

public interface IResourceMonitorReadRepository
{
    Task<HashSet<Guid>> GetGroupShiftIds(Guid groupId, CancellationToken cancellationToken);

    Task<HashSet<Guid>> GetShiftIdsForGroups(IReadOnlyCollection<Guid> groupIds, CancellationToken cancellationToken);

    Task<HashSet<Guid>> GetClientIdsForShiftsInRange(
        IReadOnlyCollection<Guid> shiftIds,
        DateOnly startDate,
        DateOnly endDate,
        CancellationToken cancellationToken);

    Task<List<ClientContract>> GetActiveContracts(
        DateOnly startDate,
        DateOnly endDate,
        IReadOnlyCollection<Guid>? clientIds,
        CancellationToken cancellationToken);

    /// <summary>
    /// The ids of the clients that count as a head (ClientTypeRules.HeadcountTypes: employees and external employees, no
    /// customer, no stand-in), among the given ids or among all clients when no ids are given.
    /// </summary>
    /// <param name="clientIds">Clients to classify, or null for all clients</param>
    /// <param name="cancellationToken">Cancels the query</param>
    Task<HashSet<Guid>> GetEmployeeClientIds(IReadOnlyCollection<Guid>? clientIds, CancellationToken cancellationToken);

    Task<HashSet<Guid>> GetContainedShiftIds(CancellationToken cancellationToken);

    Task<List<DashboardShiftRow>> GetActiveShifts(
        DateOnly startDate,
        DateOnly endDate,
        IReadOnlyCollection<Guid>? shiftIds,
        IReadOnlyCollection<Guid> containedShiftIds,
        CancellationToken cancellationToken);

    Task<List<DashboardAbsenceRow>> GetAbsences(
        DateTime periodStart,
        DateTime periodEnd,
        IReadOnlyCollection<Guid>? clientIds,
        CancellationToken cancellationToken);

    Task<string?> GetSettingValue(string type, CancellationToken cancellationToken);
}
