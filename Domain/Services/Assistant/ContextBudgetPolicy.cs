// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Derives per-turn budget caps (history messages, tool count, memory counts, tool-result size) by
/// piecewise-linear interpolation over the provider's effective input token limit. Four calibration
/// anchors define the curve: Tiny (small local/open models), Small, Reference (today's fixed values,
/// unchanged for the 100k-200k range that covers every model in production today, including
/// Anthropic's standard 200k effective limit) and Large (future >200k context, e.g. the 1M-context
/// beta). Below the lowest anchor and above the highest anchor the profile is clamped flat.
/// </summary>

using Klacks.Api.Domain.Interfaces.Assistant;
using Klacks.Api.Domain.Models.Assistant;
using Klacks.Api.Domain.Services.Assistant.Providers;

namespace Klacks.Api.Domain.Services.Assistant;

public class ContextBudgetPolicy : IContextBudgetPolicy
{
    internal const int TinyEffectiveInputTokens = 16_000;
    internal const int SmallEffectiveInputTokens = 32_000;
    internal const int ReferenceEffectiveInputTokens = 100_000;
    internal const int ReferencePlateauEndTokens = 200_000;
    internal const int LargeEffectiveInputTokens = 400_000;

    // Defensive floor: 9 always-on skills (get_current_time, get_system_info, get_user_context,
    // navigate_to, get_page_controls, get_user_permissions, search_in_list, search_and_navigate,
    // confirm_pending_action) plus deterministic guaranteed skills (page-explain, recipe steps, ...)
    // must always fit even at the smallest window. Guarded by
    // SkillToolBudgetGuardTests.AlwaysOnSkillCount_FitsWithinAdaptiveMinToolsForProvider.
    internal const int MinToolsForProvider = 12;

    // Never raised above the reference cap for quality/provider reasons — a huge context window must
    // not translate into more tool schemas than the model was tuned against. Single source of truth:
    // KnowledgeIndexConstants.MaxToolsForProvider mirrors this value (Application may depend on
    // Domain, not the reverse, so the constant is owned here).
    public const int MaxToolsForProviderCeiling = 30;

    private static readonly ContextBudgetProfile TinyProfile = new(
        MaxHistoryMessages: 8,
        MaxToolsForProvider: MinToolsForProvider,
        MaxPinnedMemories: 3,
        MaxMemoriesPerTurn: 2,
        MaxToolResultChars: 2_000);

    private static readonly ContextBudgetProfile SmallProfile = new(
        MaxHistoryMessages: 12,
        MaxToolsForProvider: 15,
        MaxPinnedMemories: 5,
        MaxMemoriesPerTurn: 3,
        MaxToolResultChars: 4_000);

    private static readonly ContextBudgetProfile ReferenceProfile = new(
        MaxHistoryMessages: 20,
        MaxToolsForProvider: MaxToolsForProviderCeiling,
        MaxPinnedMemories: 10,
        MaxMemoriesPerTurn: 5,
        MaxToolResultChars: 8_000);

    private static readonly ContextBudgetProfile LargeProfile = new(
        MaxHistoryMessages: 40,
        MaxToolsForProvider: MaxToolsForProviderCeiling,
        MaxPinnedMemories: 15,
        MaxMemoriesPerTurn: 8,
        MaxToolResultChars: 8_000);

    private static readonly (int Tokens, ContextBudgetProfile Profile)[] Anchors =
    [
        (TinyEffectiveInputTokens, TinyProfile),
        (SmallEffectiveInputTokens, SmallProfile),
        (ReferenceEffectiveInputTokens, ReferenceProfile),
        (ReferencePlateauEndTokens, ReferenceProfile),
        (LargeEffectiveInputTokens, LargeProfile),
    ];

    public ContextBudgetProfile Resolve(ILLMProvider provider, LLMModel model)
    {
        var effectiveInputLimit = provider.GetEffectiveInputTokenLimit(model);
        var interpolated = Interpolate(effectiveInputLimit);
        return Clamp(interpolated);
    }

    private static ContextBudgetProfile Interpolate(int effectiveInputLimit)
    {
        if (effectiveInputLimit <= Anchors[0].Tokens)
        {
            return Anchors[0].Profile;
        }

        if (effectiveInputLimit >= Anchors[^1].Tokens)
        {
            return Anchors[^1].Profile;
        }

        for (var i = 0; i < Anchors.Length - 1; i++)
        {
            var (lowerTokens, lowerProfile) = Anchors[i];
            var (upperTokens, upperProfile) = Anchors[i + 1];

            if (effectiveInputLimit > upperTokens)
            {
                continue;
            }

            var t = upperTokens == lowerTokens
                ? 0d
                : (double)(effectiveInputLimit - lowerTokens) / (upperTokens - lowerTokens);

            return new ContextBudgetProfile(
                MaxHistoryMessages: Lerp(lowerProfile.MaxHistoryMessages, upperProfile.MaxHistoryMessages, t),
                MaxToolsForProvider: Lerp(lowerProfile.MaxToolsForProvider, upperProfile.MaxToolsForProvider, t),
                MaxPinnedMemories: Lerp(lowerProfile.MaxPinnedMemories, upperProfile.MaxPinnedMemories, t),
                MaxMemoriesPerTurn: Lerp(lowerProfile.MaxMemoriesPerTurn, upperProfile.MaxMemoriesPerTurn, t),
                MaxToolResultChars: Lerp(lowerProfile.MaxToolResultChars, upperProfile.MaxToolResultChars, t));
        }

        return Anchors[^1].Profile;
    }

    private static int Lerp(int from, int to, double t) =>
        (int)Math.Round(from + ((to - from) * t), MidpointRounding.AwayFromZero);

    private static ContextBudgetProfile Clamp(ContextBudgetProfile profile) => new(
        MaxHistoryMessages: Math.Clamp(profile.MaxHistoryMessages, TinyProfile.MaxHistoryMessages, LargeProfile.MaxHistoryMessages),
        MaxToolsForProvider: Math.Clamp(profile.MaxToolsForProvider, MinToolsForProvider, MaxToolsForProviderCeiling),
        MaxPinnedMemories: Math.Clamp(profile.MaxPinnedMemories, TinyProfile.MaxPinnedMemories, LargeProfile.MaxPinnedMemories),
        MaxMemoriesPerTurn: Math.Clamp(profile.MaxMemoriesPerTurn, TinyProfile.MaxMemoriesPerTurn, LargeProfile.MaxMemoriesPerTurn),
        MaxToolResultChars: Math.Clamp(profile.MaxToolResultChars, TinyProfile.MaxToolResultChars, LargeProfile.MaxToolResultChars));
}
