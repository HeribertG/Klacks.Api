// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// The WorkChange types that hand (part of) a work to another person (ReplaceClientId). Such a replacement is a
/// committed assignment of the substitute: the recovery engine and the planning wizards must treat it as occupancy
/// of the substitute and never move or delete the work that carries it. Single definition for every reader.
/// </summary>
/// <param name="type">The WorkChange type to classify</param>

using Klacks.Api.Domain.Enums;

namespace Klacks.Api.Domain.Constants;

public static class ReplacementWorkChangeTypes
{
    public static readonly IReadOnlyList<WorkChangeType> All =
    [
        WorkChangeType.ReplacementStart,
        WorkChangeType.ReplacementEnd,
        WorkChangeType.ReplacementWithin,
    ];

    public static bool IsReplacement(WorkChangeType type) => All.Contains(type);

    public static bool IsReplacement(int? type) => type is { } value && IsReplacement((WorkChangeType)value);
}
