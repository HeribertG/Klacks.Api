// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Running record of one streamed chat turn: the context it runs for, the conversation and model it
/// resolved, and everything the turn has produced so far - streamed text, calls, accumulated usage and
/// timings. The streamed turn writes into it as it goes instead of keeping the values in locals, so the
/// helpers the turn is split into and the interrupted-turn safety net share one picture of the turn.
/// Registered scoped: a chat request runs exactly one turn, and Begin starts a clean record should a
/// scope ever run a second one. The outcome is claimed exactly once, by whoever ends the turn first.
/// </summary>

using System.Diagnostics;
using System.Text;
using Klacks.Api.Domain.Constants;
using Klacks.Api.Domain.Enums;
using ProviderFunctionCall = Klacks.Api.Domain.Services.Assistant.Providers.LLMFunctionCall;
using ProviderUsage = Klacks.Api.Domain.Services.Assistant.Providers.LLMUsage;

namespace Klacks.Api.Domain.Models.Assistant;

public sealed class TurnRunState
{
    private const int NoOutcome = -1;

    private int _outcome = NoOutcome;
    private int _contentLengthAtLastCalls;
    private long _startTimestamp = Stopwatch.GetTimestamp();

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

    /// <summary>Cancelled when the turn's owner asks for a stop; None when the turn offers no stop.</summary>
    public CancellationToken StopToken { get; private set; }

    public bool StopRequested => StopToken.IsCancellationRequested;

    /// <summary>Milliseconds since the turn began, read when a turn is persisted without its own clock at hand.</summary>
    public long ElapsedMs => (long)Stopwatch.GetElapsedTime(_startTimestamp).TotalMilliseconds;

    /// <summary>How the turn ended, null while it is running or when it was left mid-way.</summary>
    public TurnOutcome? Outcome
    {
        get
        {
            var value = Volatile.Read(ref _outcome);
            return value == NoOutcome ? null : (TurnOutcome)value;
        }
    }

    /// <summary>
    /// The phase the turn is in, derived from what it has produced: nothing yet, tools called without text
    /// since, or text being streamed. One of the InterruptedTurnPhases.
    /// </summary>
    public string Phase
    {
        get
        {
            var hasText = StreamedContent.Length > 0;
            if (Calls.Count == 0)
            {
                return hasText ? InterruptedTurnPhases.DuringText : InterruptedTurnPhases.BeforeText;
            }

            return StreamedContent.Length > _contentLengthAtLastCalls
                ? InterruptedTurnPhases.DuringText
                : InterruptedTurnPhases.DuringTools;
        }
    }

    /// <summary>
    /// Starts the record for a new turn, discarding whatever an earlier turn of the same scope left in it.
    /// </summary>
    /// <param name="context">The context of the turn that is about to run; its stop token becomes the turn's</param>
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
        StopToken = context.StopToken;
        _contentLengthAtLastCalls = 0;
        _startTimestamp = Stopwatch.GetTimestamp();
        Volatile.Write(ref _outcome, NoOutcome);
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

    /// <summary>
    /// Adds the calls the model made in one round and remembers where in the streamed text they were made,
    /// which is what tells the phase "tools called, no text since" from "text streamed after the tools".
    /// </summary>
    /// <param name="calls">The calls of the round, in the order the model made them</param>
    public void RegisterCalls(IReadOnlyCollection<ProviderFunctionCall> calls)
    {
        Calls.AddRange(calls);
        _contentLengthAtLastCalls = StreamedContent.Length;
    }

    /// <summary>
    /// Claims the outcome. Only the first claim succeeds, which is what keeps a turn that ended on its own
    /// from being persisted a second time by the interrupted-turn safety net.
    /// </summary>
    /// <param name="outcome">How the turn ended</param>
    /// <returns>True when this call claimed the outcome, false when the turn already had one</returns>
    public bool TrySetOutcome(TurnOutcome outcome)
    {
        return Interlocked.CompareExchange(ref _outcome, (int)outcome, NoOutcome) == NoOutcome;
    }
}
