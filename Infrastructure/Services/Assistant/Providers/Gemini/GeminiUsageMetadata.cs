// Copyright (c) Heribert Gasparoli Private. All rights reserved.

namespace Klacks.Api.Infrastructure.Services.Assistant.Providers.Gemini;

public class GeminiUsageMetadata
{
    public int PromptTokenCount { get; set; }

    public int CandidatesTokenCount { get; set; }

    public int TotalTokenCount { get; set; }
}