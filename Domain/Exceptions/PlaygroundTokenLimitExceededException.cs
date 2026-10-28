// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Raised by the Playground budget decorator when a streaming provider call is refused because one of the
/// daily token buckets (global, IP or device) is exhausted.
/// </summary>

using Klacks.Api.Domain.Constants;

namespace Klacks.Api.Domain.Exceptions;

public class PlaygroundTokenLimitExceededException : Exception
{
    public PlaygroundTokenLimitExceededException()
        : base(PlaygroundTokenLimitConstants.LimitReachedMessage)
    {
    }
}
