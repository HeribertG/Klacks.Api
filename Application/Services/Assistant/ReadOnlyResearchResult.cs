// Copyright (c) Heribert Gasparoli Private. All rights reserved.

namespace Klacks.Api.Application.Services.Assistant;

public record ReadOnlyResearchResult(
    string Synthesis,
    int IterationsUsed,
    int ToolCallCount,
    IReadOnlyList<string> ToolsUsed,
    bool ModelAvailable);
