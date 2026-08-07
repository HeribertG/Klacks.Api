// Copyright (c) Heribert Gasparoli Private. All rights reserved.

namespace Klacks.Api.Application.Exceptions;

/// <summary>
/// A period close was refused because the period still holds unresolved errors. Carries the number of
/// errors the period holds right now so the caller can confirm exactly that state: a confirmation is
/// checked against the count it was issued for, and findings that appeared in the meantime are
/// reported again instead of being sealed over unseen.
/// </summary>
/// <param name="message">Human-readable reason, mapped into the 409 body</param>
/// <param name="currentErrorCount">Unresolved errors the period holds at the moment of the refusal</param>
public sealed class PeriodValidationConflictException : ConflictException
{
    /// <summary>Machine-readable discriminator on the 409 body, so a client can tell this conflict apart.</summary>
    public const string ErrorCode = "PERIOD_VALIDATION_CONFLICT";

    public PeriodValidationConflictException(string message, int currentErrorCount)
        : base(message)
    {
        CurrentErrorCount = currentErrorCount;
    }

    public int CurrentErrorCount { get; }
}
