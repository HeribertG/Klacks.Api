// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// One change of the macro reference of one shift or absence type made by the assistant, kept so that the change can be
/// undone: which holder (Target, TargetId), the macro before (PreviousMacroId, null when the holder had none) and after
/// (NewMacroId, null when an undo removed the reference), and who asked for it (ChangedByUserId; the time is the
/// CreateTime the context stamps on insert). All rows written by one switch share its SwitchId — a switch of a shift
/// writes one row per shift cut from the same order — and the switch id is what the assistant reports and what an undo
/// addresses. An undo is itself recorded as new rows with a new shared switch id; each points at the row it undid
/// (RevertOfHistoryId), and each undone row points back (RevertedByHistoryId). There is deliberately no foreign key: the
/// history outlives deleted macros, shifts and absence types.
/// </summary>

using Klacks.Api.Domain.Common;
using Klacks.Api.Domain.Enums;

namespace Klacks.Api.Domain.Models.Settings;

public class MacroAssignmentHistory : BaseEntity
{
    public Guid SwitchId { get; set; }

    public MacroAssignmentTarget Target { get; set; }

    public Guid TargetId { get; set; }

    public Guid? PreviousMacroId { get; set; }

    public Guid? NewMacroId { get; set; }

    public Guid ChangedByUserId { get; set; }

    public Guid? RevertOfHistoryId { get; set; }

    public Guid? RevertedByHistoryId { get; set; }
}
