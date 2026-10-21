// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// A single planned row the compliance partition refused to accept.
/// </summary>
/// <param name="Index">Index of the row in the caller's original row list</param>
/// <param name="ReasonKey">Translation key of the rule violation that blocked the row</param>

namespace Klacks.Api.Application.DTOs.Schedules;

public sealed record BlockedRow(int Index, string ReasonKey);
