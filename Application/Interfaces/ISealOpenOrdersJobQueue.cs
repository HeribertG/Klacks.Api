// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Application.DTOs.Orders;

namespace Klacks.Api.Application.Interfaces;

/// <summary>
/// Queue for seal_open_orders batches too large to seal synchronously within a chat turn. Enqueue
/// only after the sync/async decision has already been made against a preview count identical to the
/// one apply=false reports, never as a second sizing algorithm.
/// </summary>
public interface ISealOpenOrdersJobQueue
{
    /// <summary>
    /// Enqueues a bulk-sealing job for background processing. Returns false when the queue is full — the
    /// caller must then surface a real error instead of a job id nobody will ever act on.
    /// </summary>
    bool Enqueue(SealOpenOrdersJob job);
}
