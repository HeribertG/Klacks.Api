// Copyright (c) Heribert Gasparoli Private. All rights reserved.

namespace Klacks.Api.Application.Interfaces;

public interface IShiftSearchRepository
{
    Task<ShiftSearchResult> SearchAsync(
        string? searchTerm = null,
        int limit = 10,
        CancellationToken cancellationToken = default);
}

public record ShiftSearchResult
{
    public required IReadOnlyList<ShiftSearchItem> Items { get; init; }
    public int TotalCount { get; init; }
}

public record ShiftSearchItem
{
    public Guid Id { get; init; }
    public DateOnly FromDate { get; init; }
    public string? ClientFirstName { get; init; }
    public string? ClientLastName { get; init; }
}
