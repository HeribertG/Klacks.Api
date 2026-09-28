// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

namespace Klacks.Api.Application.Services.Schedules;

public sealed record JobTerminalState<TResult>(
    bool Found,
    string Status,
    TResult? Result,
    string? Reason)
    where TResult : class;
