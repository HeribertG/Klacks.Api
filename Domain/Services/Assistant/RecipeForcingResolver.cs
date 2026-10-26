// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Matches a chat message to an operator-authored recipe and returns its deterministic forcing plan.
/// The "dienst-aus-bestellung-schneiden" recipe engages only when the message (a) is not a question or an
/// edit of an existing thing, and (b) expresses a create intent AND a split intent AND a topic anchor
/// (Dienst/Schicht/Bestellung/...). Intents are matched at word boundaries so mid-word false friends
/// ("Pflege" -> "lege", "mitteilen" -> "teile") no longer trigger. The full chain is
/// find_customer_candidates -> create_shift (open order) -> seal_shift -> cut_shift; when the message already carries
/// the customer as a GUID, the customer step is skipped and the clientId is pre-seeded so the chain runs from
/// create_shift. The request turn ends on the held seal; ResumeAfterSealConfirmation builds the plan of the turn that
/// confirms it (waits for the confirmed replay, then forces the cut with the request quoted in its step note).
/// When no recipe matches, returns null and the normal chat path runs unchanged.
/// </summary>
/// <param name="message">The current user chat message; matched case-insensitively.</param>

using System.Globalization;
using System.Text.RegularExpressions;
using Klacks.Api.Domain.Constants;

namespace Klacks.Api.Domain.Services.Assistant;

public static partial class RecipeForcingResolver
{
    private const int CreateShiftStepIndex = 1;

    private const int SealShiftStepIndex = 2;

    private static readonly Regex CreateIntentRegex = BuildWordStartRegex(RecipeConstants.CreateIntentKeywords);

    private static readonly Regex SplitIntentRegex = BuildWordStartRegex(RecipeConstants.SplitIntentKeywords);

    private static readonly Regex AnchorWordBoundaryRegex = BuildWordStartRegex(RecipeConstants.AnchorWordBoundaryKeywords);

    public static RecipeForcingPlan? Resolve(string? message)
    {
        if (!Matches(message))
        {
            return null;
        }

        var steps = BuildSteps(RecipeConstants.CutShiftStepNote);

        var guidMatch = GuidPattern().Match(message!);
        if (guidMatch.Success)
        {
            // Customer already identified: skip the lookup and run the chain from create_shift.
            return new RecipeForcingPlan(
                RecipeConstants.CutFromOrderRecipeName, steps,
                startIndex: CreateShiftStepIndex, initialClientId: guidMatch.Value);
        }

        return new RecipeForcingPlan(RecipeConstants.CutFromOrderRecipeName, steps);
    }

    /// <summary>
    /// The plan of the turn that confirms the recipe's held seal: it starts on the seal step, waits for the confirmed
    /// replay of the seal of exactly this order and then forces cut_shift on the plannable shift the seal created. The cut
    /// step note quotes the request that started the recipe, because the parts are only in that request.
    /// </summary>
    /// <param name="originalRequest">The user message that engaged the recipe in the previous turn.</param>
    /// <param name="orderId">The id of the order the previous turn created (create_shift, Data.ShiftId).</param>
    public static RecipeForcingPlan ResumeAfterSealConfirmation(string originalRequest, string orderId)
    {
        var cutNote = string.Format(
            CultureInfo.InvariantCulture,
            RecipeConstants.CutShiftContinuationStepNoteTemplate,
            RecipeCorrectionComposer.CapForStorage(originalRequest));

        return new RecipeForcingPlan(
            RecipeConstants.CutFromOrderRecipeName, BuildSteps(cutNote),
            startIndex: SealShiftStepIndex, initialOrderId: orderId, awaitsConfirmedReplay: true);
    }

    /// <summary>True when the message engages the recipe (create intent, split intent and topic anchor, no question or edit).</summary>
    public static bool Matches(string? message) =>
        !string.IsNullOrWhiteSpace(message) && MatchesCutFromOrder(message);

    public static IReadOnlyList<string> GuaranteedSkillNames(string? message)
    {
        if (!Matches(message))
        {
            return [];
        }

        return
        [
            RecipeConstants.FindCustomerSkill,
            RecipeConstants.CreateShiftSkill,
            RecipeConstants.SealShiftSkill,
            RecipeConstants.CutShiftSkill,
        ];
    }

    private static RecipeForcingStep[] BuildSteps(string cutStepNote) =>
    [
        new(RecipeConstants.FindCustomerSkill, RecipeConstants.FindCustomerStepNote, CapturesCustomer: true),
        new(RecipeConstants.CreateShiftSkill, RecipeConstants.CreateShiftStepNote, NeedsCustomerId: true, CapturesOrder: true),
        new(RecipeConstants.SealShiftSkill, RecipeConstants.SealShiftStepNote, NeedsOrderId: true, SealsOrder: true),
        new(RecipeConstants.CutShiftSkill, cutStepNote, NeedsPlannableShiftId: true),
    ];

    private static bool MatchesCutFromOrder(string message)
    {
        if (IsQuestionOrEdit(message))
        {
            return false;
        }

        var hasCreate = CreateIntentRegex.IsMatch(message);
        var hasSplit = SplitIntentRegex.IsMatch(message)
            || RecipeConstants.SplitIntentPhrases
                .Any(p => message.Contains(p, StringComparison.OrdinalIgnoreCase));
        var hasAnchor = AnchorWordBoundaryRegex.IsMatch(message)
            || RecipeConstants.AnchorKeywords
                .Any(a => message.Contains(a, StringComparison.OrdinalIgnoreCase));

        return hasCreate && hasSplit && hasAnchor;
    }

    private static bool IsQuestionOrEdit(string message)
    {
        var trimmed = message.TrimStart();

        return RecipeConstants.RequestBlockingOpeners
                .Any(o => trimmed.StartsWith(o, StringComparison.OrdinalIgnoreCase))
            || RecipeConstants.InformationalMarkers
                .Any(m => message.Contains(m, StringComparison.OrdinalIgnoreCase));
    }

    private static Regex BuildWordStartRegex(IReadOnlyList<string> stems) =>
        new(@"\b(?:" + string.Join('|', stems) + ")",
            RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

    [GeneratedRegex(
        "[0-9a-fA-F]{8}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{12}")]
    private static partial Regex GuidPattern();
}
