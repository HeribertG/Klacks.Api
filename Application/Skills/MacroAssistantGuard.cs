// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Server-side ownership guard for the assistant's macro skills, and the one place that holds their refusal
/// texts. The assistant may delete only macros it owns (origin Assistant or AssistantExtension). It may change
/// macros it created itself (Assistant) freely; of an extended copy (AssistantExtension) it may change the name and
/// the description, never the script, because the regression check that proved the original output is preserved
/// ran on exactly that script. Templates shipped with Klacks, region-setup imports and user-created macros are
/// refused with an actionable InvalidRequestException. Assigning an assistant-owned macro while an order is created is
/// refused as well; the switch goes through the macro assignment skill with its server-computed preview. The admin REST
/// path does not use this guard.
/// </summary>
/// <param name="macro">The macro a skill is about to change, delete or assign, as loaded from the database</param>
/// <param name="changesScript">Update only: whether the update replaces the script with a different one</param>

using System.Globalization;
using Klacks.Api.Application.DTOs.Settings;
using Klacks.Api.Domain.Enums;
using Klacks.Api.Domain.Exceptions;
using Klacks.Api.Domain.Extensions;

namespace Klacks.Api.Application.Skills;

public static class MacroAssistantGuard
{
    private const string UpdateRejectedMessage =
        "Macro '{0}' {1}. The assistant may only change macros it created itself. To add behaviour, create an "
        + "extended copy of this macro instead; changing the original is reserved for an administrator in the "
        + "macro settings.";

    private const string DeleteRejectedMessage =
        "Macro '{0}' {1}. The assistant may only delete macros it created itself; deleting this macro is "
        + "reserved for an administrator in the macro settings.";

    private const string ExtensionScriptChangeRejectedMessage =
        "Macro '{0}' is an extended copy made by the assistant; its script cannot be changed, because the check "
        + "that proved the original output is preserved ran on exactly this script. Changes are only possible via a "
        + "new extend_macro on the original macro (and deleting this copy if it is no longer needed). The name and "
        + "the description of this copy may still be changed.";

    private const string AssignmentRejectedMessage =
        "Calculation macro '{0}' (id {1}) was created by the assistant and is not assigned while an order is created. "
        + "Create the order without macroId (it gets the standard shift macro), then switch its macro with "
        + MacroAssignmentSkillNames.AssignToShift
        + ", which switches every shift cut from the same order, shows the affected works and a dry run and needs "
        + "an administrator's confirmation.";

    private const string SeedOriginDescription = "is a template shipped with Klacks";
    private const string ImportOriginDescription = "was imported by the region setup";
    private const string UserOriginDescription = "was created by a user";

    public static void EnsureMayUpdate(MacroResource macro, bool changesScript)
    {
        if (macro.Origin == MacroOrigin.Assistant)
        {
            return;
        }

        if (macro.Origin == MacroOrigin.AssistantExtension)
        {
            if (changesScript)
            {
                throw new InvalidRequestException(Format(ExtensionScriptChangeRejectedMessage, macro.Name));
            }

            return;
        }

        throw new InvalidRequestException(Format(UpdateRejectedMessage, macro.Name, DescribeOrigin(macro.Origin)));
    }

    public static void EnsureMayDelete(MacroResource macro)
    {
        if (macro.Origin.IsAssistantOwned())
        {
            return;
        }

        throw new InvalidRequestException(Format(DeleteRejectedMessage, macro.Name, DescribeOrigin(macro.Origin)));
    }

    public static string? FindAssignmentRefusal(MacroResource macro) =>
        macro.Origin.IsAssistantOwned() ? Format(AssignmentRejectedMessage, macro.Name, macro.Id) : null;

    private static string DescribeOrigin(MacroOrigin origin) => origin switch
    {
        MacroOrigin.Seed => SeedOriginDescription,
        MacroOrigin.Import => ImportOriginDescription,
        _ => UserOriginDescription
    };

    private static string Format(string format, params object[] args) =>
        string.Format(CultureInfo.InvariantCulture, format, args);
}
