// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Command for updating synonyms for a navigation target, persisting to the database.
/// </summary>
/// <param name="TargetId">The unique identifier of the navigation target to update</param>
/// <param name="Locale">The locale whose synonyms are being updated (e.g. "de", "en", "fr")</param>
/// <param name="Synonyms">The new set of synonyms for this locale</param>
/// <param name="Status">The new synonym status (e.g. "generated", "approved")</param>

namespace Klacks.Api.Application.Handlers.Klacksy;

using Klacks.Api.Infrastructure.Mediator;

public record UpdateNavigationTargetSynonymsCommand(string TargetId, string Locale, string[] Synonyms, string Status) : IRequest<bool>;
