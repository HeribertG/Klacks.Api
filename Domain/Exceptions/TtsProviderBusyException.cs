// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

namespace Klacks.Api.Domain.Exceptions;

/// <summary>
/// A text-to-speech provider refused a request because it is busy (the local voice answers 429 when its queue is full).
/// The caller may try again after RetryAfter, when the provider named a delay.
/// </summary>
/// <param name="retryAfter">Delay the provider asked for, or null when it named none</param>
public sealed class TtsProviderBusyException : Exception
{
    public TtsProviderBusyException(TimeSpan? retryAfter)
        : base("The text-to-speech provider is busy.")
    {
        RetryAfter = retryAfter;
    }

    public TimeSpan? RetryAfter { get; }
}
