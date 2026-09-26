// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Reads the parameters of the macro assignment skills the same way for the confirmation preview and for the confirmed
/// call. Holders, macros and switches are addressed by id only: the gate replays the stored parameters after the
/// confirmation, and a name resolved twice (once for the preview, once for the replayed call) could land on different rows.
/// A value that is not an id is echoed flattened and shortened by <see cref="MacroAssignmentNames"/>.
/// </summary>
/// <param name="parameters">The raw invocation arguments</param>
/// <param name="target">Whether the holder id is a shift id or an absence type id</param>

using System.Globalization;
using Klacks.Api.Domain.Enums;
using Klacks.Api.Domain.Models.Macros;
using Klacks.Api.Domain.Services.Assistant.Skills;
using Klacks.Api.Domain.Services.Macros;

namespace Klacks.Api.Application.Skills;

public static class MacroAssignmentParameters
{
    public const string ShiftId = "shiftId";
    public const string AbsenceTypeId = "absenceTypeId";
    public const string MacroId = "macroId";
    public const string SwitchId = "switchId";

    private const string MissingIdMessage =
        "{0} is required: pass the id of the {1}. Names are not accepted; look the {1} up first.";
    private const string InvalidIdMessage = "'{0}' is not a valid id for {1}.";
    private const string MacroNoun = "macro";
    private const string MissingSwitchIdMessage =
        "switchId is required: pass the switch id reported when the macro was switched. A shift or absence type id cannot "
        + "identify the switch to undo, because that holder may have been switched again since.";

    public static (Guid HolderId, Guid MacroId, string? Error) ReadAssign(
        Dictionary<string, object> parameters, MacroAssignmentTarget target)
    {
        var holderParameter = target == MacroAssignmentTarget.Shift ? ShiftId : AbsenceTypeId;
        var (holderId, holderError) = ReadRequired(parameters, holderParameter, MacroAssignmentPolicy.NounOf(target));
        if (holderError != null)
        {
            return (Guid.Empty, Guid.Empty, holderError);
        }

        var (macroId, macroError) = ReadRequired(parameters, MacroId, MacroNoun);
        return macroError != null ? (Guid.Empty, Guid.Empty, macroError) : (holderId, macroId, null);
    }

    public static (MacroRevertRequest? Request, string? Error) ReadRevert(Dictionary<string, object> parameters)
    {
        var raw = SkillParameterReader.Read<string>(parameters, SwitchId);
        if (string.IsNullOrWhiteSpace(raw))
        {
            return (null, MissingSwitchIdMessage);
        }

        return Guid.TryParse(raw, out var switchId)
            ? (new MacroRevertRequest(switchId), null)
            : (null, DescribeInvalid(raw, SwitchId));
    }

    private static (Guid Id, string? Error) ReadRequired(
        Dictionary<string, object> parameters, string name, string noun)
    {
        var raw = SkillParameterReader.Read<string>(parameters, name);
        if (string.IsNullOrWhiteSpace(raw))
        {
            return (Guid.Empty, Format(MissingIdMessage, name, noun));
        }

        return Guid.TryParse(raw, out var id) ? (id, null) : (Guid.Empty, DescribeInvalid(raw, name));
    }

    private static string DescribeInvalid(string raw, string name) =>
        Format(InvalidIdMessage, MacroAssignmentNames.Safe(raw), name);

    private static string Format(string format, params object[] args) =>
        string.Format(CultureInfo.InvariantCulture, format, args);
}
