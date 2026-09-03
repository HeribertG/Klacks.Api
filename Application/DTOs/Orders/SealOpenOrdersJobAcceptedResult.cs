// Copyright (c) Heribert Gasparoli Private. All rights reserved.

namespace Klacks.Api.Application.DTOs.Orders;

/// <summary>
/// Returned by seal_open_orders instead of a SealOpenOrdersResult when the batch exceeds
/// SealOpenOrdersSkill.SealOpenOrdersSynchronousLimit: the sealing itself runs as a background job and
/// this is the immediate acknowledgement, not the outcome.
/// </summary>
/// <param name="JobId">Id of the queued job; also carried by the inbox message posted once it finishes.</param>
/// <param name="PlannedCount">Sealable order count from the same preview count used for the sync/async decision — the job may end up sealing more or fewer if the data changes before it runs.</param>
public sealed record SealOpenOrdersJobAcceptedResult(Guid JobId, int PlannedCount);
