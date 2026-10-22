// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

using Klacks.Api.Application.Commands.Orders;

namespace Klacks.Api.Application.DTOs.Orders;

/// <summary>
/// Payload queued for SealOpenOrdersJobBackgroundService: the exact command the synchronous path would
/// have sent to IMediator, plus the identity of the user who asked for it (there is no HTTP request to
/// read that from once the job runs on the background thread) and a job id the caller can quote back to
/// the user immediately, before the job has even started.
/// </summary>
/// <param name="JobId">Correlates the immediate chat reply with the inbox message the job posts later.</param>
/// <param name="UserId">The user seal_open_orders was called for; the inbox message targets this user.</param>
/// <param name="Command">Unmodified apply=true command, run through the same IMediator handler as the synchronous path.
/// It carries the caller's OpenOrderSealingScope (full-access flag and visible group ids) resolved at request time, so
/// the handler re-filters the orders as they are when the job runs instead of sealing everything it finds.</param>
public sealed record SealOpenOrdersJob(Guid JobId, Guid UserId, SealOpenOrdersCommand Command);
