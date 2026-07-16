// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Domain.Models.Scheduling;

namespace Klacks.Api.Domain.Interfaces.Schedules;

public interface IOvertimeConfigResolver
{
    /// <summary>
    /// Resolves the ONE overtime definition of the system for a client and date: the
    /// SchedulingRule-ladder / dated-revision-snapshot / OVERTIME_*-settings /
    /// EffectiveContractData.OvertimeThreshold fallback chain that the K3/K4 surcharge engine and the
    /// OvertimeHours period cap share. An empty Tiers list on the result means overtime is not
    /// configured for this client/date via any of the ladder sources — callers may still fall back to
    /// the contract's OvertimeThreshold for a cap-only threshold, but must never invent a second
    /// overtime definition.
    /// </summary>
    /// <param name="clientId">The client whose contract/rule chain anchors the resolution</param>
    /// <param name="date">The date the resolved ladder must be valid for (dated revisions are snapshots per ValidFrom)</param>
    Task<OvertimeSurchargeConfig> ResolveAsync(Guid clientId, DateOnly date);
}
