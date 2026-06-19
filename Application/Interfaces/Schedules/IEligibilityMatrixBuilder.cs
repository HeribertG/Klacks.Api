// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Application.Services.Schedules;

namespace Klacks.Api.Application.Interfaces.Schedules;

public interface IEligibilityMatrixBuilder
{
    Task<EligibilityMatrix> BuildAsync(
        IReadOnlyCollection<Guid> agentIds,
        IReadOnlyCollection<EligibilitySlot> slots,
        CancellationToken ct = default);
}
