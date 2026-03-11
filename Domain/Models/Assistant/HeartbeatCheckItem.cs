// Copyright (c) Heribert Gasparoli Private. All rights reserved.

namespace Klacks.Api.Domain.Models.Assistant;

public sealed record HeartbeatCheckItem(
    string Label,
    string? Category,
    bool IsEnabled);
