// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Running record of one streamed chat turn: the context it runs for, the conversation and model it
/// resolved, and everything the turn has produced so far - streamed text, calls, accumulated usage and
/// timings. The streamed turn writes into it as it goes instead of keeping the values in locals, so the
/// helpers the turn is split into share one picture of the turn.
/// </summary>

using System.Text;
using ProviderFunctionCall = Klacks.Api.Domain.Services.Assistant.Providers.LLMFunctionCall;
using ProviderUsage = Klacks.Api.Domain.Services.Assistant.Providers.LLMUsage;

namespace Klacks.Api.Domain.Models.Assistant;

public sealed class TurnRunState
{
    public LLMContext? Context { get; private set; }

    public LLMConversation? Conversation { get; private set; }

    public LLMModel? Model { get; private set; }

    public bool ProviderSupportsToolChoice { get; private set; }

    public StringBuilder StreamedContent { get; private set; } = new();

    public List<ProviderFunctionCall> Calls { get; private set; } = new();

    public ProviderUsage Usage { get; private set; } = new();

    public long? TtftMs { get; set; }

    public int ToolIterations { get; set; }

    public bool ToolChoiceRequested { get; set; }

    public bool AnsweredWithNotice { get; set; }

    public string? NavigationRoute { get; set; }

    public string? NavigationTarget { get; set; }

    /// <summary>
    /// Starts the record for a new turn, discarding whatever an earlier turn of the same scope left in it.
    /// </summary>
    /// <param name="context">The context of the turn that is about to run</param>
    public void Begin(LLMContext context)
    {
        Context = context;
        Conversation = null;
        Model = null;
        ProviderSupportsToolChoice = false;
        StreamedContent = new StringBuilder();
        Calls = new List<ProviderFunctionCall>();
        Usage = new ProviderUsage();
        TtftMs = null;
        ToolIterations = 0;
        ToolChoiceRequested = false;
        AnsweredWithNotice = false;
        NavigationRoute = null;
        NavigationTarget = null;
    }

    /// <summary>
    /// Records the conversation and model the turn resolved once its context was prepared.
    /// </summary>
    /// <param name="conversation">The persisted conversation the turn belongs to</param>
    /// <param name="model">The model the turn runs on</param>
    /// <param name="providerSupportsToolChoice">Whether the provider honours tool_choice, recorded on the usage row</param>
    public void Attach(LLMConversation conversation, LLMModel model, bool providerSupportsToolChoice)
    {
        Conversation = conversation;
        Model = model;
        ProviderSupportsToolChoice = providerSupportsToolChoice;
    }
}
