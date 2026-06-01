// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Domain.Models.Assistant;

namespace Klacks.Api.Application.Interfaces.Assistant;

/// <summary>
/// Application-layer enrichment that resolves the per-client scheduling scope for an LLM turn. When the
/// turn is a scheduling task and a single client is in scope (PageContext.SelectedClientId), it resolves
/// that client's effective scheduling policy and stamps it onto the LLMContext, so the (Domain, pure)
/// rule-context provider can render a tightly-scoped per-client limits block. Lives in the Application
/// layer because the policy resolver is an Application interface; the Domain pipeline never depends on it.
/// </summary>
public interface IPlanningScopeEnricher
{
    /// <summary>
    /// Resolves and stamps <see cref="LLMContext.ScopedClientPolicy"/> when applicable; no-op otherwise.
    /// Never throws on malformed page context.
    /// </summary>
    Task EnrichAsync(LLMContext context, CancellationToken cancellationToken = default);
}
