// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Query for retrieving all navigation targets, optionally filtered by synonym status and locale.
/// </summary>
/// <param name="Status">Optional synonym status filter (e.g. "pending", "generated", "approved")</param>
/// <param name="Locale">Optional locale filter (reserved for future use)</param>

namespace Klacks.Api.Application.Handlers.Klacksy;

using Klacks.Api.Application.Klacksy.Models;
using Klacks.Api.Infrastructure.Mediator;

public record GetNavigationTargetsQuery(string? Status, string? Locale) : IRequest<IReadOnlyList<NavigationTarget>>;
