// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Application.DTOs.Scheduling;

namespace Klacks.Api.Application.Interfaces.Scheduling;

/// <summary>
/// Read surface for the consequences of an ACTIVE_INDUSTRIES change. The setting only filters
/// selection lists, so switching it leaves every existing assignment untouched; this reports which
/// contracts are affected so the switch becomes visible instead of silently doing nothing.
/// </summary>
public interface IIndustryMigrationReader
{
    /// <summary>
    /// Contracts referencing a scheduling rule whose industry is not among the active ones. Rules
    /// without an industry tag are customer-owned and never reported.
    /// </summary>
    Task<IReadOnlyList<IndustryMigrationCandidate>> GetContractsOnInactiveIndustriesAsync(
        CancellationToken cancellationToken = default);
}
