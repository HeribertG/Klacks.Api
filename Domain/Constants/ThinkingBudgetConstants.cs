// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Values of LLMProviderRequest.ThinkingBudgetTokens with a fixed meaning for every provider.
/// Disabled asks the provider to switch the model's thinking off for one short, plain answer (greeting,
/// recipe confirmation and ask steps, empty-answer recovery, speech and transcription helpers).
/// </summary>

namespace Klacks.Api.Domain.Constants;

public static class ThinkingBudgetConstants
{
    public const int Disabled = 0;
}
