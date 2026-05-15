// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Constants related to shift graph traversal and lifecycle bounds.
/// </summary>
namespace Klacks.Api.Application.Constants;

public static class ShiftConstants
{
    /// <summary>
    /// Maximum depth the OriginalId descendant resolver walks. A SealedOrder
    /// clones into an OriginalShift which can be re-split repeatedly; 8 levels
    /// covers every realistic shift tree while preventing runaway loops on
    /// pathological data.
    /// </summary>
    public const int MaxOriginalIdDescendantDepth = 8;
}
