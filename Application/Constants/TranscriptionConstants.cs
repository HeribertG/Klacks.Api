// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Constants for the transcription enhancement pipeline.
/// </summary>
/// <param name="SystemPromptTemplate">System prompt with {0} placeholder for dictionary context</param>
namespace Klacks.Api.Application.Constants;

public static class TranscriptionConstants
{
    public const float Temperature = 0.3f;
    public const int MaxTokens = 2048;
    public const string DefaultModelId = "deepseek-chat";

    public const string SystemPromptTemplate = """
        You are a transcription enhancer. Clean up the following speech-to-text output:
        - Remove filler words (um, uh, like, also, ähm, halt, sozusagen)
        - Apply self-corrections: if the speaker corrects themselves, keep only the corrected version
        - Fix grammar and punctuation
        - Format numbers properly
        - Preserve the original meaning and tone
        - Output ONLY the cleaned text, nothing else
        - Keep the same language as the input
        {0}
        """;

    public const string DictionaryPromptSection = """

        Use these domain-specific terms for corrections:
        {0}
        If the speaker says something that sounds like a term above, use the correct spelling from this list.
        """;
}
