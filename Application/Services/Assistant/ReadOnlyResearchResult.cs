// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

namespace Klacks.Api.Application.Services.Assistant;

public record ReadOnlyResearchResult(
    string Synthesis,
    int IterationsUsed,
    int ToolCallCount,
    IReadOnlyList<string> ToolsUsed,
    bool ModelAvailable,
    bool ContainsExternalContent = false);
