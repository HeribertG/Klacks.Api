// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Handler for retrieving unresolved navigation feedback entries for a given locale.
/// </summary>

namespace Klacks.Api.Application.Handlers.Klacksy;

using Klacks.Api.Application.Interfaces.Klacksy;
using Klacks.Api.Domain.Models.Klacksy;
using Klacks.Api.Infrastructure.Mediator;

public sealed class GetNavigationFeedbackQueryHandler : IRequestHandler<GetNavigationFeedbackQuery, IReadOnlyList<KlacksyNavigationFeedback>>
{
    private readonly IKlacksyNavigationFeedbackRepository _repo;

    public GetNavigationFeedbackQueryHandler(IKlacksyNavigationFeedbackRepository repo) => _repo = repo;

    public Task<IReadOnlyList<KlacksyNavigationFeedback>> Handle(GetNavigationFeedbackQuery query, CancellationToken cancellationToken)
        => _repo.QueryUnresolvedAsync(query.Locale, query.Take, cancellationToken);
}
