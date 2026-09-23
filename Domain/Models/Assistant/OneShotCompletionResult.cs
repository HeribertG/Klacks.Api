// Copyright (c) Heribert Gasparoli Private. All rights reserved.

namespace Klacks.Api.Domain.Models.Assistant;

/// <summary>
/// Outcome of one pipeline-free LLM completion (IOneShotCompletionService). Success=false means the call
/// itself failed (no model/provider, provider error after transient retries, or an exception) and Error
/// carries the reason; Content is then empty. Success=true only states that the provider answered — the
/// caller still has to validate the content (it may be empty or not in the requested format).
/// </summary>
/// <param name="Success">True when the provider returned a successful response</param>
/// <param name="Content">The model's text reply; empty on failure</param>
/// <param name="Error">Why the call failed; null on success</param>
public sealed record OneShotCompletionResult(bool Success, string Content, string? Error)
{
    public static OneShotCompletionResult Succeeded(string content) => new(true, content, null);

    public static OneShotCompletionResult Failed(string error) => new(false, string.Empty, error);
}
