// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Refusal texts of a container split.
/// </summary>

namespace Klacks.Api.Domain.Constants;

public static class ContainerSplitMessages
{
    public const string OutsideContainer = "The split time must lie inside the container.";

    public const string CutsThroughItem = "A container can only be split between two of its tasks or absences, never through one.";

    public const string BothPartsNeedATask = "Both parts of a split container must keep at least one task.";

    public const string SplitTimeInWholeMinutes = "The split time must be given in whole minutes.";

    public const string NotLockedBySession = "Cannot split: container work is not locked by this session.";

    public const string NotAContainer = "Only a container can be split.";

    public const string RecalculationFailed =
        "The split container could not be recalculated, so it was not split. Check the macro of its shift.";
}
