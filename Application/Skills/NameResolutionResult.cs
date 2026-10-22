// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Outcome of a staged by-name resolution: either a single unambiguous match, a single suggestion
/// that only a fuzzy stage found (Strict policy - the caller must ask the user instead of acting on
/// it), a list of candidates that need user disambiguation, or nothing (all empty) when no name is
/// plausible.
/// </summary>

namespace Klacks.Api.Application.Skills;

internal sealed record NameResolutionResult<T>
    where T : class
{
    public T? Match { get; private init; }

    public T? Suggestion { get; private init; }

    public IReadOnlyList<T> Candidates { get; private init; } = Array.Empty<T>();

    public static NameResolutionResult<T> Single(T item) => new() { Match = item };

    public static NameResolutionResult<T> Suggested(T item) => new() { Suggestion = item };

    public static NameResolutionResult<T> Ambiguous(IReadOnlyList<T> candidates) => new() { Candidates = candidates };

    public static NameResolutionResult<T> NotFound() => new();
}
