// Copyright (c) Heribert Gasparoli Private. All rights reserved.

namespace Klacks.Api.Application.Services.Schedules;

public interface IEligibilityMatrixBuilder
{
    Task<EligibilityMatrix> BuildAsync(
        IReadOnlyCollection<Guid> agentIds,
        IReadOnlyCollection<EligibilitySlot> slots,
        CancellationToken ct = default);
}
