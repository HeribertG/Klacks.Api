// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

namespace Klacks.Api.Application.Interfaces;

public record GroupSearchItem
{
    public Guid Id { get; init; }
    public string? Name { get; init; }
}
