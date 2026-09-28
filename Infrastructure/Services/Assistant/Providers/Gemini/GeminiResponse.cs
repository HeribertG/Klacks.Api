// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

namespace Klacks.Api.Infrastructure.Services.Assistant.Providers.Gemini;

public class GeminiResponse
{
    public List<GeminiCandidate> Candidates { get; set; } = new();

    public GeminiUsageMetadata? UsageMetadata { get; set; }
}