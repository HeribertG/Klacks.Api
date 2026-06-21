// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Matches a chat message to an operator-authored recipe and returns its deterministic forcing plan.
/// The "dienst-aus-bestellung-schneiden" recipe engages when the message expresses BOTH a create intent
/// and a split intent. The full chain is find_customer_candidates -> create_shift -> cut_shift; when the
/// message already carries the customer as a GUID, the customer step is skipped and the clientId is
/// pre-seeded so the chain runs from create_shift. When no recipe matches, returns null and the normal
/// chat path runs unchanged.
/// </summary>
/// <param name="message">The current user chat message; matched case-insensitively.</param>

using System.Text.RegularExpressions;
using Klacks.Api.Domain.Constants;

namespace Klacks.Api.Domain.Services.Assistant;

public static partial class RecipeForcingResolver
{
    private const int CreateShiftStepIndex = 1;

    public static RecipeForcingPlan? Resolve(string? message)
    {
        if (string.IsNullOrWhiteSpace(message) || !MatchesCutFromOrder(message))
        {
            return null;
        }

        var steps = new[]
        {
            new RecipeForcingStep(RecipeConstants.FindCustomerSkill, RecipeConstants.FindCustomerStepNote, CapturesCustomer: true),
            new RecipeForcingStep(RecipeConstants.CreateShiftSkill, RecipeConstants.CreateShiftStepNote, NeedsCustomerId: true),
            new RecipeForcingStep(RecipeConstants.CutShiftSkill, RecipeConstants.CutShiftStepNote),
        };

        var guidMatch = GuidPattern().Match(message);
        if (guidMatch.Success)
        {
            // Customer already identified: skip the lookup and run the chain from create_shift.
            return new RecipeForcingPlan(
                RecipeConstants.CutFromOrderRecipeName, steps,
                startIndex: CreateShiftStepIndex, initialClientId: guidMatch.Value);
        }

        return new RecipeForcingPlan(RecipeConstants.CutFromOrderRecipeName, steps);
    }

    public static IReadOnlyList<string> GuaranteedSkillNames(string? message)
    {
        if (string.IsNullOrWhiteSpace(message) || !MatchesCutFromOrder(message))
        {
            return [];
        }

        return
        [
            RecipeConstants.FindCustomerSkill,
            RecipeConstants.CreateShiftSkill,
            RecipeConstants.CutShiftSkill,
        ];
    }

    private static bool MatchesCutFromOrder(string message)
    {
        var hasCreate = RecipeConstants.CreateIntentKeywords
            .Any(k => message.Contains(k, StringComparison.OrdinalIgnoreCase));
        var hasSplit = RecipeConstants.SplitIntentKeywords
            .Any(k => message.Contains(k, StringComparison.OrdinalIgnoreCase));

        return hasCreate && hasSplit;
    }

    [GeneratedRegex(
        "[0-9a-fA-F]{8}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{12}")]
    private static partial Regex GuidPattern();
}
