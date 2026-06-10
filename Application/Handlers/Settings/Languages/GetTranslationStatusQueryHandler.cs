// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Reports whether the DeepL translation service is configured with an API key.
/// </summary>
/// <param name="translationService">Translation service exposing the configuration state.</param>

using Klacks.Api.Application.Queries.Settings.Languages;
using Klacks.Api.Domain.Interfaces.Translation;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Handlers.Settings.Languages;

public class GetTranslationStatusQueryHandler : IRequestHandler<GetTranslationStatusQuery, bool>
{
    private readonly ITranslationService _translationService;

    public GetTranslationStatusQueryHandler(ITranslationService translationService)
    {
        _translationService = translationService;
    }

    public async Task<bool> Handle(GetTranslationStatusQuery request, CancellationToken cancellationToken)
    {
        return await _translationService.IsConfiguredAsync();
    }
}
