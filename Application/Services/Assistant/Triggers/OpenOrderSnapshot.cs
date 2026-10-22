// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

namespace Klacks.Api.Application.Services.Assistant.Triggers;

/// <param name="ShiftId">The open order (a shift in status OriginalOrder)</param>
/// <param name="FromDate">First day of the order; decides whether it starts within the soon window</param>
/// <param name="GroupIds">Every group the order belongs to; empty when it belongs to none (Admins only)</param>
public sealed record OpenOrderSnapshot(
    Guid ShiftId,
    DateOnly FromDate,
    IReadOnlyCollection<Guid> GroupIds);
