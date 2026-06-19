// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Query for retrieving unresolved navigation feedback entries for a given locale.
/// </summary>
/// <param name="Locale">The locale to filter feedback by</param>
/// <param name="Take">Maximum number of feedback entries to return</param>

namespace Klacks.Api.Application.Handlers.Klacksy;

using Klacks.Api.Domain.Models.Klacksy;
using Klacks.Api.Infrastructure.Mediator;

public record GetNavigationFeedbackQuery(string Locale, int Take) : IRequest<IReadOnlyList<KlacksyNavigationFeedback>>;
