// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

namespace Klacks.Api.Application.DTOs.Schedules;

/// <summary>
/// One client and the day its state is judged on, for a check of several clients in one call.
/// </summary>
/// <param name="ClientId">Employee to judge</param>
/// <param name="AsOfDate">Day the client's state is judged on</param>
public sealed record ClientAsOfDate(Guid ClientId, DateOnly AsOfDate);
