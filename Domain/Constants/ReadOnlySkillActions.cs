// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Explicit catalogue of multi-action skills whose name carries no read-only prefix but whose individual
/// actions include pure reads. The multi-turn loop runs a side-effecting skill at most once per turn; for
/// these skills that rule applies per call, not per name: a call whose "action" argument is listed here
/// is treated like a read-only skill, so it may repeat and does not block a later write action of the same
/// skill. An entry may only be added after verifying in the skill implementation that the action performs
/// no write. The empty string stands for an absent or blank action and is listed only where the skill
/// itself resolves a missing action to a read.
/// </summary>
using Klacks.Api.Domain.Services.Assistant.Skills;

namespace Klacks.Api.Domain.Constants;

public static class ReadOnlySkillActions
{
    public const string ActionParameter = "action";

    public const string AbsentAction = "";

    public const string PendingNotesRead = "read";

    private static readonly IReadOnlyDictionary<string, IReadOnlySet<string>> Catalogue =
        new Dictionary<string, IReadOnlySet<string>>(StringComparer.OrdinalIgnoreCase)
        {
            [SkillNames.ManagePendingNotes] = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                PendingNotesRead,
                AbsentAction
            }
        };

    /// <summary>
    /// True when the call targets a catalogued skill with one of its read-only actions.
    /// </summary>
    /// <param name="skillName">Name of the called skill.</param>
    /// <param name="parameters">Raw call arguments; the action value may still be a JsonElement.</param>
    public static bool IsReadOnlyCall(string? skillName, Dictionary<string, object>? parameters)
    {
        if (string.IsNullOrEmpty(skillName) || !Catalogue.TryGetValue(skillName, out var readOnlyActions))
        {
            return false;
        }

        var action = parameters == null
            ? null
            : SkillParameterReader.Read<string>(parameters, ActionParameter);

        return readOnlyActions.Contains(action?.Trim() ?? AbsentAction);
    }
}
