// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Decides whether a turn that redeems a held action continues the recipe "dienst-aus-bestellung-schneiden". The recipe's
/// request turn ends on the held seal_shift (sealing needs the user's confirmation), and a forcing plan lives for one turn
/// only, so without this the confirmation turn would seal and then force nothing. It continues only when all of these
/// hold: the redeemed action is seal_shift, the immediately preceding turn's record is not superseded, its user message
/// engages the recipe (RecipeForcingResolver.Matches) and it created an order through create_shift. The plan it returns
/// waits for the confirmed replay of the seal of exactly that order and then forces cut_shift; the plan compares the
/// sealed order with the created one, so a token of another order seals and continues nothing.
/// Pure: the caller reads the record and the redeemed skill.
/// </summary>
/// <param name="redeemedSkillName">The skill of the held action this turn is forced to confirm, null when none is forced.</param>
/// <param name="previousTurn">The previous-action record of the conversation (the recipe's request turn).</param>

using System.Text.Json;
using Klacks.Api.Domain.Constants;
using Klacks.Api.Domain.Extensions;
using Klacks.Api.Domain.Models.Assistant;

namespace Klacks.Api.Domain.Services.Assistant;

public static class CutRecipeContinuationResolver
{
    public static RecipeForcingPlan? Resolve(string? redeemedSkillName, AssistantLastAction? previousTurn)
    {
        if (!string.Equals(redeemedSkillName, RecipeConstants.SealShiftSkill, StringComparison.OrdinalIgnoreCase)
            || previousTurn is not { SupersededAtUtc: null }
            || !RecipeForcingResolver.Matches(previousTurn.UserMessage))
        {
            return null;
        }

        var orderId = CreatedOrderId(previousTurn);
        return orderId == null
            ? null
            : RecipeForcingResolver.ResumeAfterSealConfirmation(previousTurn.UserMessage, orderId);
    }

    private static string? CreatedOrderId(AssistantLastAction previousTurn)
    {
        var create = previousTurn.Calls.LastOrDefault(call =>
            call.Success && string.Equals(call.SkillName, RecipeConstants.CreateShiftSkill, StringComparison.OrdinalIgnoreCase));
        if (create == null || string.IsNullOrWhiteSpace(create.ResultDataJson))
        {
            return null;
        }

        try
        {
            using var document = JsonDocument.Parse(create.ResultDataJson);
            if (document.RootElement.ValueKind != JsonValueKind.Object
                || !document.RootElement.TryGetPropertyCaseInsensitive(RecipeConstants.CreatedOrderIdDataProperty, out var id)
                || id.ValueKind != JsonValueKind.String)
            {
                return null;
            }

            var value = id.GetString();
            return string.IsNullOrWhiteSpace(value) ? null : value;
        }
        catch (JsonException)
        {
            return null;
        }
    }
}
