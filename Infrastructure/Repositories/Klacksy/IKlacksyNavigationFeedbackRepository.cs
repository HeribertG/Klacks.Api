// Copyright (c) Heribert Gasparoli Private. All rights reserved.

namespace Klacks.Api.Infrastructure.Repositories.Klacksy;

using Klacks.Api.Domain.Models.Klacksy;

public interface IKlacksyNavigationFeedbackRepository
{
    Task AddAsync(KlacksyNavigationFeedback entity, CancellationToken ct);
    Task<IReadOnlyList<KlacksyNavigationFeedback>> QueryUnresolvedAsync(string locale, int take, CancellationToken ct);
    Task<IReadOnlyList<KlacksyNavigationFeedback>> QueryByUtterancePatternAsync(string pattern, int take, CancellationToken ct);
}
