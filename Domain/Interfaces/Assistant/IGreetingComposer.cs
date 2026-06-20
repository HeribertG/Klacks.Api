// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Domain.Models.Assistant;

namespace Klacks.Api.Domain.Interfaces.Assistant;

/// <summary>
/// Composes one warm, witty, localized opening greeting that weaves only grounded ambient facts
/// (weather, a public holiday, an optional light local note). Returns null on any failure or when
/// no model is available — the caller then falls back to the static template greeting.
/// </summary>
public interface IGreetingComposer
{
    Task<string?> ComposeAsync(GreetingContext context, CancellationToken cancellationToken = default);
}
