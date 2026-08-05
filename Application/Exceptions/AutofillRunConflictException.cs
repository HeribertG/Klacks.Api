// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Domain.Enums;

namespace Klacks.Api.Application.Exceptions;

/// <summary>
/// The identical run is already in progress. Starting it twice would burn the time budget on a plan
/// that a second job is about to overwrite anyway, and both results would race for the same apply.
/// </summary>
/// <param name="runningJobId">Job that already holds the lock, so the client can attach to it.</param>
/// <param name="family">Autofill family the run belongs to.</param>
public sealed class AutofillRunConflictException : ConflictException
{
    public AutofillRunConflictException(Guid runningJobId, AutofillFamily family)
        : base($"A {family} run for the same selection is already in progress.")
    {
        RunningJobId = runningJobId;
        Family = family;
    }

    public Guid RunningJobId { get; }

    public AutofillFamily Family { get; }
}
