// Copyright (c) Heribert Gasparoli Private. All rights reserved.

namespace Klacks.Api.Application.Interfaces;

public interface IGroupSearchRepository
{
    Task<GroupSearchResult> SearchAsync(
        string? searchTerm = null,
        int limit = 10,
        CancellationToken cancellationToken = default);
}

public record GroupSearchResult
{
    public required IReadOnlyList<GroupSearchItem> Items { get; init; }
    public int TotalCount { get; init; }
}

public record GroupSearchItem
{
    public Guid Id { get; init; }
    public string? Name { get; init; }
}
