// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Repository interface for ContainerShiftOverride CRUD and lookup operations.
/// </summary>
using Klacks.Api.Domain.Models.Schedules;

namespace Klacks.Api.Domain.Interfaces.Schedules;

public interface IContainerShiftOverrideRepository
{
    Task<ContainerShiftOverride?> GetByContainerAndDate(Guid containerId, DateOnly date, CancellationToken ct = default);
    Task<ContainerShiftOverride?> GetByContainerAndDateWithItems(Guid containerId, DateOnly date, CancellationToken ct = default);
    Task<List<ContainerShiftOverride>> GetByContainerAndDateRange(Guid containerId, DateOnly fromDate, DateOnly toDate, CancellationToken ct = default);
    Task<ContainerShiftOverride?> GetWithTracking(Guid overrideId, CancellationToken ct = default);
    Task Add(ContainerShiftOverride entity);
    Task Delete(ContainerShiftOverride entity);
    Task<bool> HasWorkForOverride(Guid containerId, DateOnly date, CancellationToken ct = default);
}
