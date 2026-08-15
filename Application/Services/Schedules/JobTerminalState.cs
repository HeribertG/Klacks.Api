// Copyright (c) Heribert Gasparoli Private. All rights reserved.

namespace Klacks.Api.Application.Services.Schedules;

public sealed record JobTerminalState<TResult>(
    bool Found,
    string Status,
    TResult? Result,
    string? Reason)
    where TResult : class;
