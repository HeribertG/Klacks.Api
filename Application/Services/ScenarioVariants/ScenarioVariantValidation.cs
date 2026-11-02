// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Input rules for creating and changing a variant (key): a name, a group when creating, and a time period that does
/// not end before it starts. A violation is answered with an InvalidRequestException (400).
/// </summary>
/// <param name="name">Display name of the variant</param>
/// <param name="groupId">Group the variant belongs to</param>
/// <param name="fromDate">Start of the time period</param>
/// <param name="untilDate">End of the time period</param>
/// <param name="shiftIds">Shifts to copy into a variant; counts as given only when it holds at least one id</param>
/// <param name="sourceGroupId">Group whose sealed shifts are copied into a variant; exactly one of this and the shift ids must be given</param>

using Klacks.Api.Domain.Exceptions;

namespace Klacks.Api.Application.Services.ScenarioVariants;

public static class ScenarioVariantValidation
{
    public const string NameRequiredMessage = "A variant needs a name";

    public const string GroupRequiredMessage = "A variant needs a group";

    public const string PeriodInvalidMessage = "The end of a variant's period must not be before its start";

    public const string ShiftsOutsidePeriodMessage = "The period cannot be changed: shifts of the variant would lie outside it";

    public const string CopySelectionMessage = "Give either the shifts to copy or a source group, exactly one of them";

    public const string SourceGroupOutsideVariantMessage = "The source group must be the variant's group or one of its subgroups";

    public static List<Guid>? EnsureCopySelection(IReadOnlyList<Guid>? shiftIds, Guid? sourceGroupId)
    {
        var hasShifts = shiftIds is { Count: > 0 };

        if (hasShifts == sourceGroupId.HasValue)
        {
            throw new InvalidRequestException(CopySelectionMessage);
        }

        return hasShifts ? shiftIds!.Distinct().ToList() : null;
    }

    public static void EnsureGroup(Guid groupId)
    {
        if (groupId == Guid.Empty)
        {
            throw new InvalidRequestException(GroupRequiredMessage);
        }
    }

    public static void EnsureDetails(string? name, DateOnly fromDate, DateOnly untilDate)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new InvalidRequestException(NameRequiredMessage);
        }

        if (untilDate < fromDate)
        {
            throw new InvalidRequestException(PeriodInvalidMessage);
        }
    }
}
