// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Outcome of a staged by-name resolution: either a single unambiguous match, a list of
/// candidates that need user disambiguation, or nothing (both empty) when no name is plausible.
/// </summary>

namespace Klacks.Api.Application.Skills;

internal sealed record NameResolutionResult<T>
    where T : class
{
    public T? Match { get; private init; }

    public IReadOnlyList<T> Candidates { get; private init; } = Array.Empty<T>();

    public static NameResolutionResult<T> Single(T item) => new() { Match = item };

    public static NameResolutionResult<T> Ambiguous(IReadOnlyList<T> candidates) => new() { Candidates = candidates };

    public static NameResolutionResult<T> NotFound() => new();
}
