// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// How a provider behaves when a turn forces a tool call (tool_choice=required). Declared per request by
/// every provider, because the answer can depend on the model and on the provider's own configuration -
/// never on a model name list kept in this code. NotSupported is the value a provider gets by default, so
/// a provider that forgets to declare is treated as the weakest case instead of silently losing the call.
/// </summary>

namespace Klacks.Api.Domain.Enums;

public enum ForcedToolChoiceSupport
{
    /// <summary>
    /// The provider does not send the forcing at all, or the API rejects it and nothing the provider can
    /// do inside the request changes that. The turn has to fall back to narrowing plus verification.
    /// </summary>
    NotSupported = 0,

    /// <summary>The provider sends the forcing and the API honours it as it stands.</summary>
    Native = 1,

    /// <summary>
    /// The API honours the forcing only while the model's thinking mode is off, so the provider turns
    /// thinking off for this one request. It stays on for every request that forces nothing.
    /// </summary>
    RequiresThinkingDisabled = 2
}
