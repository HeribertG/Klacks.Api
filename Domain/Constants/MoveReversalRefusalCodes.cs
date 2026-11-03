// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

namespace Klacks.Api.Domain.Constants;

/// <summary>
/// Machine codes of a refused move reversal, answered with 400 next to the English detail, so a client can explain the
/// refusal in the user's language. A row or work outside the caller's visibility has no code: it reads like a missing one.
/// </summary>
public static class MoveReversalRefusalCodes
{
    /// <summary>The service is back with the person it came from, or was never moved.</summary>
    public const string AlreadyBack = "MOVE_ALREADY_BACK";

    /// <summary>The service moved on since; only its last move can be taken back.</summary>
    public const string MovedOn = "MOVE_MOVED_ON";

    /// <summary>Something holds the service in place: it is sealed or replaced.</summary>
    public const string Held = "MOVE_HELD";

    /// <summary>The person the service came from is not free at its time: another service or a blocking absence.</summary>
    public const string OriginBusy = "MOVE_ORIGIN_BUSY";

    /// <summary>The row is no move that reached the plan.</summary>
    public const string NotAnAppliedMove = "MOVE_NOT_APPLIED";

    /// <summary>The move of this row was already taken back.</summary>
    public const string AlreadyReversed = "MOVE_ALREADY_REVERSED";
}
