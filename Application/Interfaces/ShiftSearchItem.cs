// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

using Klacks.Api.Domain.Enums;

namespace Klacks.Api.Application.Interfaces;

public record ShiftSearchItem
{
    public Guid Id { get; init; }
    public DateOnly FromDate { get; init; }
    public string? Abbreviation { get; init; }
    public string? Name { get; init; }
    public ShiftStatus Status { get; init; }
    public string? ClientFirstName { get; init; }
    public string? ClientLastName { get; init; }
}
