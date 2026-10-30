// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

namespace Klacks.Api.Application.DTOs.Schedules;

/// <summary>
/// A service that stays out of the tour.
/// </summary>
/// <param name="ShiftId">The service</param>
/// <param name="ClientName">Client of the service</param>
/// <param name="Reason">Why it stays out</param>
public sealed record ContainerBundlingLeftOut(Guid ShiftId, string ClientName, ContainerBundlingLeftOutReason Reason);
