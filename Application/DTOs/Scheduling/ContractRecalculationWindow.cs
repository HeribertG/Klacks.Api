// Copyright (c) Heribert Gasparoli Private. All rights reserved.

namespace Klacks.Api.Application.DTOs.Scheduling;

/// <summary>
/// Validity window of a contract that references a changed scheduling rule.
/// </summary>
/// <param name="ContractId">The referencing contract</param>
/// <param name="ValidFrom">Start of the contract validity</param>
/// <param name="ValidUntil">End of the contract validity; null for open-ended contracts</param>
public sealed record ContractRecalculationWindow(Guid ContractId, DateOnly ValidFrom, DateOnly? ValidUntil);
