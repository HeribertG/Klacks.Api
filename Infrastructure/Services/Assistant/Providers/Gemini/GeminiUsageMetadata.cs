// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

namespace Klacks.Api.Infrastructure.Services.Assistant.Providers.Gemini;

public class GeminiUsageMetadata
{
    public int PromptTokenCount { get; set; }

    public int CandidatesTokenCount { get; set; }

    public int TotalTokenCount { get; set; }
}