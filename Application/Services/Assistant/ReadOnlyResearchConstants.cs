// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Tuning constants and the system prompt for the read-only research sub-loop. Kept in one place so the
/// iteration budget, tool-call budget and token limits are not scattered as magic numbers across the
/// service.
/// </summary>

namespace Klacks.Api.Application.Services.Assistant;

public static class ReadOnlyResearchConstants
{
    public const string RunAnalysisSkillName = "run_analysis";

    // Upper bound on tool-calling round-trips with the cheap model before a final synthesis pass is
    // forced. Analysis questions typically fan out over a handful of read-only lookups, so a small cap
    // keeps latency and cost bounded while still allowing several data-gathering steps.
    public const int MaxIterations = 5;

    // Hard cap on the total number of read-only tool executions across all iterations. Protects against a
    // model that keeps requesting lookups; once hit, the loop stops calling tools and synthesizes.
    public const int MaxToolCalls = 12;

    // Response token budget per model call (both the tool-calling turns and the final synthesis turn).
    public const int MaxResponseTokens = 1200;

    // Low temperature: the sub-loop analyzes data and reports facts, so deterministic behaviour is
    // preferred over creative variance.
    public const double Temperature = 0.2;

    // Per-tool-result cap fed back into the sub-loop history, so one large read result cannot inflate the
    // running prompt beyond the cheap model's input window.
    public const int MaxToolResultChars = 1500;

    public const string SystemPrompt =
        "You are a read-only research assistant working INSIDE a larger assistant. You have been given a " +
        "single analysis question and a set of READ-ONLY tools that only look up data (they never change " +
        "anything). Use the tools to gather the facts you need, then write ONE compact English synthesis " +
        "that answers the question directly. Rules: only use the provided tools; never claim to have " +
        "changed, created or deleted anything; do not navigate; if the tools cannot answer the question, " +
        "say so plainly and state what is missing. Keep the final answer short and factual — it will be " +
        "handed back to the outer assistant as a tool result, so omit pleasantries and focus on findings.";

    public const string SynthesisInstruction =
        "Stop calling tools now. Based only on the data gathered above, write the final compact English " +
        "synthesis that answers the original question.";

    public const string NoModelAvailableMessage =
        "Read-only research is unavailable because no enabled LLM model is configured.";

    public const string NoFindingsMessage =
        "The read-only research sub-loop produced no findings for the question.";
}
