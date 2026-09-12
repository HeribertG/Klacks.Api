// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Size limits for persisted proactive trigger dispatch rows, shared between the EF column
/// configuration and the trigger dispatch service so serialized content params can never
/// overflow the database column.
/// </summary>

namespace Klacks.Api.Domain.Constants;

public static class ProactiveTriggerDispatchLimits
{
    /// <summary>
    /// Dedup key column width. Bounded on purpose and NOT widened to text: the column takes part in
    /// two unique btree indexes, whose per-tuple size limit would turn an unbounded key into a later,
    /// harder-to-read insert failure.
    /// </summary>
    public const int DedupKeyMaxLength = 512;

    /// <summary>
    /// Content key column width. Display text the inbox renders, so it stays a short text rather than
    /// growing into a full message body.
    /// </summary>
    public const int ContentKeyMaxLength = 512;

    public const int ContentParamsJsonMaxLength = 4000;

    public const int ContentParamValueMaxLength = 1000;

    public const int ActionRouteMaxLength = 256;

    public const int ActionParamsJsonMaxLength = 2000;

    public const string TruncationSuffix = "…";
}
