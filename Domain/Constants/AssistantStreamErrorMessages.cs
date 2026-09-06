// Copyright (c) Heribert Gasparoli Private. All rights reserved.

namespace Klacks.Api.Domain.Constants;

/// <summary>
/// Texts the assistant stream sends to the browser when it fails. Deliberately generic: the real
/// exception is logged server-side, and its message must never travel to the client.
/// </summary>
public static class AssistantStreamErrorMessages
{
    public const string UnexpectedFailure = "The assistant could not complete this response. Please try again.";

    public const string ContextPreparationFailure = "The assistant could not prepare this conversation. Please try again.";

    public const string ProviderFailure = "The language model did not answer. Please try again.";
}
