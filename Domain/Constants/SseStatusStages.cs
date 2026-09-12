// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Machine-readable stage keys carried by SseChunkType.Status events. The browser shows progress
/// long before the first content token arrives, so the keys must stay stable: the frontend maps them
/// to localized text itself. The backend deliberately never sends display text.
/// </summary>

namespace Klacks.Api.Domain.Constants;

public static class SseStatusStages
{
    /// <summary>
    /// The turn is loading the agent, resolving its model and assembling the skill toolset. Measured
    /// at 4-26 s in development, so this is the stage the user waits on most.
    /// </summary>
    public const string AssemblingToolset = "assembling_toolset";

    /// <summary>
    /// Conversation history, soul/memory prompt and system prompt are being built.
    /// </summary>
    public const string PreparingContext = "preparing_context";

    /// <summary>
    /// The recipe engine is resolving or resuming a guided flow for this message.
    /// </summary>
    public const string ResolvingRecipe = "resolving_recipe";

    /// <summary>
    /// The request is with the language model; the next content token ends this stage.
    /// </summary>
    public const string CallingModel = "calling_model";

    /// <summary>
    /// The model requested tools and they are being executed before it answers.
    /// </summary>
    public const string ExecutingTool = "executing_tool";
}
