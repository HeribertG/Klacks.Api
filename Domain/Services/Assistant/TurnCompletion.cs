// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Everything the persistence tail of a finished streamed turn needs to store it: the conversation and
/// model the turn ran on, the answer as the user saw it, the accumulated usage, timings and calls.
/// </summary>
/// <param name="Context">The turn context (user, message, turn id, toolset assembly timing).</param>
/// <param name="Conversation">The persisted conversation the turn belongs to.</param>
/// <param name="Model">The model the turn ran on.</param>
/// <param name="ProviderSupportsToolChoice">Whether the provider honoured tool_choice, recorded on the usage row.</param>
/// <param name="ResponseContent">The answer as stored, including any closing notices.</param>
/// <param name="Usage">Token and cost totals accumulated over all model calls of the turn.</param>
/// <param name="ElapsedMs">Wall-clock duration of the turn in milliseconds.</param>
/// <param name="TtftMs">Milliseconds until the first streamed token, null when none arrived.</param>
/// <param name="ToolIterations">Number of tool-loop iterations the turn ran.</param>
/// <param name="FunctionCalls">Every call of the turn, including rejected and held ones.</param>
/// <param name="ToolChoiceRequested">True when tool_choice=required was sent on any iteration.</param>
/// <param name="RecipePausedOnAsk">True when the turn left a recipe waiting on an ask or confirmation step.</param>
/// <param name="AnsweredWithNotice">True when the stored answer is a canned notice rather than a model answer.</param>

using Klacks.Api.Domain.Models.Assistant;
using Klacks.Api.Domain.Services.Assistant.Providers;

namespace Klacks.Api.Domain.Services.Assistant;

public sealed record TurnCompletion(
    LLMContext Context,
    LLMConversation Conversation,
    LLMModel Model,
    bool ProviderSupportsToolChoice,
    string ResponseContent,
    Providers.LLMUsage Usage,
    long ElapsedMs,
    long? TtftMs,
    int ToolIterations,
    List<LLMFunctionCall> FunctionCalls,
    bool ToolChoiceRequested,
    bool RecipePausedOnAsk,
    bool AnsweredWithNotice);
