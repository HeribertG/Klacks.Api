// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Application.Interfaces;
using Klacks.Api.Domain.Models.Staffs;

namespace Klacks.Api.Application.Services.Schedules;

/// <summary>
/// Default <see cref="IAvailabilityIneligibilityService"/>: loads the availability rows for the slots'
/// date span once (date-keyed, so no timestamptz/UTC concerns), keeps only the requested agents, and
/// delegates the time-overlap matching to <see cref="AvailabilityMatrixBuilder"/>.
/// </summary>
/// <param name="availabilityRepository">Source of hour-granular client availability</param>
public sealed class AvailabilityIneligibilityService : IAvailabilityIneligibilityService
{
    private readonly IClientAvailabilityRepository _availabilityRepository;

    public AvailabilityIneligibilityService(IClientAvailabilityRepository availabilityRepository)
    {
        _availabilityRepository = availabilityRepository;
    }

    public async Task<IReadOnlySet<(string AgentId, Guid ShiftId, DateOnly Date)>> GetAsync(
        IReadOnlyList<Guid> agentIds,
        IReadOnlyList<AvailabilityShiftSlot> slots,
        CancellationToken ct)
    {
        if (agentIds.Count == 0 || slots.Count == 0)
        {
            return new HashSet<(string, Guid, DateOnly)>();
        }

        var from = slots.Min(s => s.Date);
        var until = slots.Max(s => s.Date);
        var rows = await _availabilityRepository.GetByDateRange(from, until);

        var agentSet = agentIds.ToHashSet();
        var byAgent = rows
            .Where(r => agentSet.Contains(r.ClientId))
            .GroupBy(r => r.ClientId.ToString())
            .ToDictionary(g => g.Key, g => (IReadOnlyList<ClientAvailability>)g.ToList());

        return AvailabilityMatrixBuilder.Build(slots, byAgent);
    }
}
