// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

namespace Klacks.Api.Application.Interfaces;

public record ClientSearchItem
{
    public Guid Id { get; init; }
    public int? IdNumber { get; init; }
    public string? FirstName { get; init; }
    public string? LastName { get; init; }
    public string? Company { get; init; }
    public string? Gender { get; init; }
    public string? EntityType { get; init; }
    public string? Canton { get; init; }
    public string? City { get; init; }
}
