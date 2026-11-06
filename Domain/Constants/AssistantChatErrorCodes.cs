// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

namespace Klacks.Api.Domain.Constants;

/// <summary>
/// Machine-readable codes of chat error events (SseChunk.ErrorCode). The English ErrorMessage stays the readable
/// summary for logs and API clients; the UI shows a translated text per code. Mirrored in
/// Klacks.Ui/src/app/domain/constants/assistant-chat-error.constants.ts (AssistantChatErrorCodesUiMirrorGuardTests).
/// </summary>
public static class AssistantChatErrorCodes
{
    public const string ProviderUnavailable = "LLM_PROVIDER_UNAVAILABLE";
}
