// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

namespace Klacks.Api.Domain.Enums;

/// <summary>
/// What the user wants from a chat turn. The order is the class order of the trained turn-intent head.
/// </summary>
public enum TurnIntentLabel
{
    /// <summary>Wants knowledge or an explanation in prose.</summary>
    Ask = 0,

    /// <summary>Wants the system to act now: show, list, open, navigate, create, change, delete, check.</summary>
    Do = 1,

    /// <summary>Greeting, thanks, praise, smalltalk, farewell, identity questions, anything off-topic.</summary>
    Other = 2
}
