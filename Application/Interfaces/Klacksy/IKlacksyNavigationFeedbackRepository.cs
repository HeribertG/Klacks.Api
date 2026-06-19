// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Domain.Models.Klacksy;

namespace Klacks.Api.Application.Interfaces.Klacksy;

public interface IKlacksyNavigationFeedbackRepository
{
    Task AddAsync(KlacksyNavigationFeedback entity, CancellationToken ct);
    Task<IReadOnlyList<KlacksyNavigationFeedback>> QueryUnresolvedAsync(string locale, int take, CancellationToken ct);
    Task<IReadOnlyList<KlacksyNavigationFeedback>> QueryByUtterancePatternAsync(string pattern, int take, CancellationToken ct);
}
