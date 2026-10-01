// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Translates the subject and body of one received email. An email whose sender belongs only to clients
/// outside the caller's group visibility is answered exactly like a missing email; nothing is translated.
/// </summary>
/// <param name="emailQueryRepository">Resolves the clients that own the sender address</param>
/// <param name="clientVisibilityGuard">Decides whether the calling user may see those clients</param>

using Klacks.Api.Application.DTOs.Email;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Application.Queries.Email;
using Klacks.Api.Domain.Interfaces.Email;
using Klacks.Api.Domain.Interfaces.Translation;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Handlers.Email;

public class TranslateReceivedEmailQueryHandler : BaseHandler, IRequestHandler<TranslateReceivedEmailQuery, TranslatedEmailResource?>
{
    private readonly IReceivedEmailRepository _repository;
    private readonly IEmailQueryRepository _emailQueryRepository;
    private readonly IClientVisibilityGuard _clientVisibilityGuard;
    private readonly ITranslationService _translationService;

    public TranslateReceivedEmailQueryHandler(
        IReceivedEmailRepository repository,
        IEmailQueryRepository emailQueryRepository,
        IClientVisibilityGuard clientVisibilityGuard,
        ITranslationService translationService,
        ILogger<TranslateReceivedEmailQueryHandler> logger)
        : base(logger)
    {
        _repository = repository;
        _emailQueryRepository = emailQueryRepository;
        _clientVisibilityGuard = clientVisibilityGuard;
        _translationService = translationService;
    }

    public async Task<TranslatedEmailResource?> Handle(TranslateReceivedEmailQuery request, CancellationToken cancellationToken)
    {
        var email = await ExecuteAsync(
            () => _repository.GetByIdAsync(request.Id),
            nameof(TranslateReceivedEmailQuery),
            new { request.Id });

        if (email == null
            || await ReceivedEmailVisibility.IsHiddenAsync(_emailQueryRepository, _clientVisibilityGuard, email, cancellationToken))
        {
            return null;
        }

        var subjectResult = await _translationService.TranslateAsync(email.Subject, null, request.TargetLanguage);

        string? translatedBodyHtml = null;
        string? translatedBodyText = null;

        if (!string.IsNullOrWhiteSpace(email.BodyHtml))
        {
            var bodyResult = await _translationService.TranslateAsync(email.BodyHtml, null, request.TargetLanguage, isHtml: true);
            translatedBodyHtml = bodyResult.TranslatedText;
        }
        else if (!string.IsNullOrWhiteSpace(email.BodyText))
        {
            var bodyResult = await _translationService.TranslateAsync(email.BodyText, null, request.TargetLanguage);
            translatedBodyText = bodyResult.TranslatedText;
        }

        return new TranslatedEmailResource
        {
            Subject = subjectResult.TranslatedText,
            BodyHtml = translatedBodyHtml,
            BodyText = translatedBodyText,
            TargetLanguage = request.TargetLanguage,
        };
    }
}
