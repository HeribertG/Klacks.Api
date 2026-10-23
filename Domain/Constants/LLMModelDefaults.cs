// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

namespace Klacks.Api.Domain.Constants;

/// <summary>
/// Model defaults of the LLM provider orchestration that do not come from the database.
/// </summary>
public static class LLMModelDefaults
{
    public const string FallbackModelId = "claude-sonnet-5";

    public const double DefaultTemperature = 0.7;

    public const int DefaultRequestMaxTokens = 2000;

    public const int DefaultModelMaxTokens = 4096;

    public const int DefaultContextWindow = 128000;
}
