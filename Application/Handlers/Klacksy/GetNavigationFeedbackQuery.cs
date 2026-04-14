// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Query and handler for retrieving unresolved navigation feedback entries for a given locale.
/// </summary>
/// <param name="Locale">The locale to filter feedback by</param>
/// <param name="Take">Maximum number of feedback entries to return</param>
namespace Klacks.Api.Application.Handlers.Klacksy;

using Klacks.Api.Domain.Models.Klacksy;
using Klacks.Api.Infrastructure.Mediator;
using Klacks.Api.Infrastructure.Repositories.Klacksy;

public record GetNavigationFeedbackQuery(string Locale, int Take) : IRequest<IReadOnlyList<KlacksyNavigationFeedback>>;

public sealed class GetNavigationFeedbackQueryHandler : IRequestHandler<GetNavigationFeedbackQuery, IReadOnlyList<KlacksyNavigationFeedback>>
{
    private readonly IKlacksyNavigationFeedbackRepository _repo;

    public GetNavigationFeedbackQueryHandler(IKlacksyNavigationFeedbackRepository repo) => _repo = repo;

    public Task<IReadOnlyList<KlacksyNavigationFeedback>> Handle(GetNavigationFeedbackQuery query, CancellationToken cancellationToken)
        => _repo.QueryUnresolvedAsync(query.Locale, query.Take, cancellationToken);
}
