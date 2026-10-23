// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Token budget for the conversation history of a chat turn: derives the per-turn ceiling from the
/// provider's effective input limit, truncates the stored history into it before the turn and keeps the
/// growing running history inside it during the tool loop. Shared by both chat loops so they budget alike.
/// </summary>

using System.Text.Json;
using Klacks.Api.Domain.Constants;
using Klacks.Api.Domain.Models.Assistant;
using Klacks.Api.Domain.Services.Assistant.Providers;

namespace Klacks.Api.Domain.Services.Assistant;

internal static class ConversationHistoryBudget
{
    private const int PercentBase = 100;

    private const int MaxHistoryMessages = 20;

    // Rough characters-per-token ratio used for all local prompt-size estimates. Deliberately
    // low (conservative) so estimates over- rather than under-count real tokenizer output.
    // Internal (not private): referenced by LLMServiceHistoryBudgetTests so the test math derives
    // from the same source of truth instead of duplicating the ratio as a test-side magic number.
    internal const int CharsPerToken = 4;

    // Percentage buffer added on top of the measured tool-definition size (see
    // EstimateToolDefinitionReserveTokens). Absorbs: (1) drift between the CharsPerToken=4 heuristic
    // and each provider's real tokenizer, (2) provider-specific wrapper overhead not modeled by the
    // generic {name, description, parameters:{type, properties, required}} shape used for the estimate
    // (e.g. OpenAI's outer {"type":"function","function":{...}} envelope, Anthropic cache_control
    // blocks). 30% keeps every measured tier (Tier 12 ~3k, Tier 15 ~3.7k, Tier 30 ~6.9k raw tokens) far
    // under the old flat 15k reserve, while still pushing the reserve for the theoretical worst case
    // (30 maximum-sized catalog skills, ~16k raw tokens) above 15k - the one scenario where the old
    // flat constant was already known to be insufficient.
    internal const int ToolDefinitionSafetyMarginPercent = 30;

    // Headroom deliberately kept free on every turn so that a single recall/list answer cannot fill the
    // whole context window — leaving room for the model's reply and for follow-up interactions.
    internal const int InteractionHeadroomTokens = 8_000;

    // Extra slack absorbing tokenizer/estimate drift (our CharsPerToken estimate is approximate).
    internal const int SafetyMarginTokens = 2_000;

    // Never starve history below this, even if overhead estimates are pessimistic.
    internal const int MinHistoryBudgetTokens = 4_000;

    // Deliberately carries no counts. It is inserted at position 0, i.e. into the prompt prefix every
    // provider with automatic prefix caching (DeepSeek among them) hashes: message counts change from
    // turn to turn and from tool iteration to tool iteration, so a counted notice invalidated the cache
    // on every single call. Internal so tests assert against this exact text instead of copying it.
    internal const string TruncationNotice = "[Earlier messages truncated.]";

    // Extra budget reserved for the wrapper markers around an injected conversation-summary system message.
    private const int SummaryBudgetReserveTokens = 50;

    // Effective per-turn budget for conversation history, derived from the provider's real input limit
    // for this model. Shared by the initial truncation and the in-loop re-truncation so both use the
    // exact same ceiling. Sums the stable and volatile system-prompt segments so the budget reflects
    // the full prompt actually sent to the provider, regardless of how it is split into cache blocks.
    // availableFunctions is the toolset SkillToolsetAssembler already assembled for this turn (final
    // by the time budgeting runs in both call sites), so the tool-definition reserve reflects what is
    // actually sent instead of a pessimistic flat constant.
    // The volatile segment also carries LLMContext.CorrectionNote on a correction turn, so such a turn
    // budgets roughly 120 tokens less history than an ordinary one. That is deliberate:
    // MinHistoryBudgetTokens stays the floor and the note lives exactly one turn.
    internal static int HistoryBudgetFor(
        ILLMProvider provider, LLMModel model, string? systemPrompt, string? volatileSystemPrompt, List<LLMFunction>? availableFunctions)
    {
        return ComputeHistoryBudget(
            provider.GetEffectiveInputTokenLimit(model),
            model.MaxTokens,
            EstimateTokens(systemPrompt) + EstimateTokens(volatileSystemPrompt),
            EstimateToolDefinitionReserveTokens(availableFunctions));
    }

    // The token budget available for conversation history (and, in the multi-turn loop, the growing
    // running history + function-result message). Derived from the provider-reported EFFECTIVE input
    // limit for the model so it adapts automatically per model instead of trusting a possibly-inflated
    // nominal context window. Output room, the measured native tool-definition size and a fixed
    // interaction headroom are reserved so the context is never filled to the brim. Never goes below
    // MinHistoryBudgetTokens, even when every other reservation is pessimistic.
    internal static int ComputeHistoryBudget(int effectiveInputLimit, int maxOutputTokens, int systemPromptTokens, int toolDefinitionTokens)
    {
        var budget = effectiveInputLimit
            - maxOutputTokens
            - systemPromptTokens
            - toolDefinitionTokens
            - InteractionHeadroomTokens
            - SafetyMarginTokens;

        return Math.Max(budget, MinHistoryBudgetTokens);
    }

    // Real per-turn reserve for the native tool/function definitions attached to the request, measured
    // from the toolset SkillToolsetAssembler actually assembled for this turn instead of a fixed
    // pessimistic constant. Null or empty toolsets correctly reserve 0 tokens - if no tools are sent to
    // the provider, there is nothing to budget for, and the full input limit remains available to
    // history.
    internal static int EstimateToolDefinitionReserveTokens(List<LLMFunction>? functions)
    {
        if (functions == null || functions.Count == 0)
            return 0;

        var rawTokens = functions.Sum(EstimateToolDefinitionTokens);
        return rawTokens + (rawTokens * ToolDefinitionSafetyMarginPercent / PercentBase);
    }

    internal static List<Providers.LLMMessage> TruncateHistory(
        List<Providers.LLMMessage> history,
        int historyBudgetTokens,
        string? conversationSummary = null,
        int maxHistoryMessages = MaxHistoryMessages)
    {
        var summaryContent = ConversationSummaryCodec.RenderInner(conversationSummary);
        var hasSummary = summaryContent != null;

        var historyBudget = historyBudgetTokens;
        if (hasSummary)
        {
            historyBudget -= EstimateTokens(summaryContent) + SummaryBudgetReserveTokens;
        }

        if (history.Count <= maxHistoryMessages && !hasSummary && EstimateTokens(history) <= historyBudget)
            return history;

        var truncated = new List<Providers.LLMMessage>();
        var tokenCount = 0;

        for (var i = history.Count - 1; i >= 0; i--)
        {
            var msgTokens = EstimateTokens(history[i].Content);
            tokenCount += msgTokens;

            if (tokenCount > historyBudget || truncated.Count >= maxHistoryMessages)
                break;

            truncated.Insert(0, history[i]);
        }

        if (hasSummary)
        {
            truncated.Insert(0, new Providers.LLMMessage
            {
                Role = LLMMessageRoles.System,
                Content = $"[Conversation Summary (earlier messages)]\n{summaryContent}\n[/Conversation Summary]"
            });
        }
        else if (truncated.Count < history.Count)
        {
            truncated.Insert(0, new Providers.LLMMessage
            {
                Role = LLMMessageRoles.System,
                Content = TruncationNotice
            });
        }

        return truncated;
    }

    // Called before every provider call inside the multi-turn tool loop. The running history grows by a
    // user + assistant message each iteration and the function-result message can be large, so without
    // this the accumulated prompt can exceed the model's input limit mid-loop. Drops the oldest
    // non-system messages (a leading conversation-summary system message is preserved) until the
    // estimated running history plus the next message fits the same budget used for the initial history.
    internal static void FitRunningHistoryToBudget(
        List<Providers.LLMMessage> runningHistory,
        string nextMessage,
        int historyBudgetTokens)
    {
        var total = EstimateTokens(nextMessage) + EstimateTokens(runningHistory);

        while (total > historyBudgetTokens && runningHistory.Count > 0)
        {
            var dropIndex = runningHistory[0].Role == LLMMessageRoles.System ? 1 : 0;
            if (dropIndex >= runningHistory.Count)
                break;

            total -= EstimateTokens(runningHistory[dropIndex].Content);
            runningHistory.RemoveAt(dropIndex);
        }
    }

    private static int EstimateTokens(string? text) => string.IsNullOrEmpty(text) ? 0 : text.Length / CharsPerToken;

    private static int EstimateTokens(IEnumerable<Providers.LLMMessage> messages) => messages.Sum(m => EstimateTokens(m.Content));

    // Character-based JSON size of a single tool definition, mirroring the
    // {name, description, parameters:{type, properties, required}} shape every supported provider
    // serializes a function/tool into (Anthropic input_schema, OpenAI/DeepSeek/Groq/Gemini parameters
    // schema) so the estimate is provider-neutral rather than tuned to one API's wire format.
    private static int EstimateToolDefinitionTokens(LLMFunction function)
    {
        var json = JsonSerializer.Serialize(new
        {
            name = function.Name,
            description = function.Description,
            parameters = new
            {
                type = "object",
                properties = function.Parameters,
                required = function.RequiredParameters
            }
        });

        return EstimateTokens(json);
    }
}
