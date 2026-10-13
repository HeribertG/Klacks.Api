// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// A write was refused because the shift is no longer a draft order: a sealed order, a plannable shift or a cut piece
/// keeps its content (see LockedShiftEditPolicy). Raised for an edit that changes a locked field, for a delete, and for
/// the administrator delete of a sealed order family that is still in use (works, scenario copies, container references).
/// It is a 409 whose "errorCode" is SHIFT_LOCKED, so the client can tell it apart from other conflicts, and the skill
/// executor answers it with a plain rejection.
/// </summary>
/// <param name="message">English detail for logs, the 409 body and skill results</param>
/// <param name="details">shiftId, status and, for an edit, the names of the locked fields that differ</param>

using Klacks.Api.Domain.Enums;
using Klacks.Api.Domain.Models.Schedules;

namespace Klacks.Api.Application.Exceptions;

public sealed class ShiftLockedException : ConflictException
{
    public const string ErrorCode = "SHIFT_LOCKED";
    public const string ShiftIdKey = "shiftId";
    public const string StatusKey = "status";
    public const string LockedFieldsKey = "lockedFields";

    private const string EditMessageFormat =
        "Shift '{0}' has status {1} and is locked: {2} cannot be changed. " +
        "Only the groups, default expenses, required qualifications, macro and an end date that is still empty can be changed.";

    private const string DeleteMessageFormat =
        "Shift '{0}' has status {1} and cannot be deleted. Only a draft order (status OriginalOrder) can be deleted; " +
        "an administrator can also delete a whole sealed order while no work exists on it or its pieces.";

    private const string FamilyInUseMessageFormat =
        "Sealed order '{0}' cannot be deleted while its order family is still in use: {1}. " +
        "Remove the works from the order and all its pieces first (scenarios and containers that use them must be dealt with too).";

    private const string CutEditorHint =
        " The times and dates of a plannable shift and of its cut pieces are changed in the cut editor.";

    private const string FieldSeparator = ", ";
    private const string WorksReason = "works exist on the order or one of its pieces";
    private const string ScenarioReason = "an active scenario holds a copy of the order or one of its pieces";
    private const string ContainerReason = "a container template or container override refers to the order or one of its pieces";

    public const string InUseKey = "inUse";

    private ShiftLockedException(string message, IReadOnlyDictionary<string, object?> details)
        : base(message, ErrorCode, details)
    {
    }

    public static ShiftLockedException ForEdit(Shift stored, IReadOnlyList<string> lockedFields) =>
        new(
            string.Format(EditMessageFormat, stored.Name, stored.Status, string.Join(FieldSeparator, lockedFields))
                + (stored.Status is ShiftStatus.OriginalShift or ShiftStatus.SplitShift ? CutEditorHint : string.Empty),
            new Dictionary<string, object?>
            {
                [ShiftIdKey] = stored.Id,
                [StatusKey] = stored.Status.ToString(),
                [LockedFieldsKey] = lockedFields.ToArray()
            });

    public static ShiftLockedException ForOrderFamilyInUse(Shift stored, OrderFamilyUsage usage)
    {
        var reasons = new List<string>();
        if (usage.HasWorks)
        {
            reasons.Add(WorksReason);
        }

        if (usage.HasScenarioClones)
        {
            reasons.Add(ScenarioReason);
        }

        if (usage.HasContainerReferences)
        {
            reasons.Add(ContainerReason);
        }

        return new ShiftLockedException(
            string.Format(FamilyInUseMessageFormat, stored.Name, string.Join(FieldSeparator, reasons)),
            new Dictionary<string, object?>
            {
                [ShiftIdKey] = stored.Id,
                [StatusKey] = stored.Status.ToString(),
                [InUseKey] = reasons.ToArray()
            });
    }

    public static ShiftLockedException ForDelete(Shift stored) =>
        new(
            string.Format(DeleteMessageFormat, stored.Name, stored.Status),
            new Dictionary<string, object?>
            {
                [ShiftIdKey] = stored.Id,
                [StatusKey] = stored.Status.ToString()
            });
}
