// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Translates a received email into a target language via the mailbox translation service
/// (the inbox "translate" button) and returns the translated subject and body text. It applies the checks of
/// POST api/backend/ReceivedEmail/{id}/Translate before it sends the query: the target language must be one of
/// LanguageConfig.SupportedLanguages and the translation service must be configured; a rejected service key is reported
/// as an error, not thrown. An HTML mail comes back translated as HTML only; its body is returned as plain text.
/// </summary>
/// <param name="emailId">Required. UUID of the email (from list_emails).</param>
/// <param name="targetLanguage">Required. ISO language code to translate into (e.g. de, en, fr, it).</param>

using Klacks.Api.Domain.Interfaces.Playground;
using Klacks.Api.Domain.Constants;
using Klacks.Api.Application.DTOs.Email;
using Klacks.Api.Application.Queries.Email;
using Klacks.Api.Domain.Attributes;
using Klacks.Api.Domain.Common;
using Klacks.Api.Domain.Exceptions;
using Klacks.Api.Domain.Interfaces.Translation;
using Klacks.Api.Domain.Models.Assistant;
using Klacks.Api.Domain.Services.Assistant.Skills.Implementations;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Skills;

[SkillImplementation("translate_email")]
public class TranslateEmailSkill : BaseSkillImplementation
{
    private const int MaxBodyLength = 4000;
    private const string TruncationMarker = " … [body truncated]";
    private const string UnsupportedLanguageMessage = "Unsupported target language '{0}'. Supported: {1}.";
    private const string NotConfiguredMessage =
        "The translation service is not configured. An administrator must set the DeepL API key in the settings.";

    private readonly IMediator _mediator;
    private readonly ITranslationService _translationService;
    private readonly IPlaygroundMode _playgroundMode;

    public TranslateEmailSkill(IMediator mediator, ITranslationService translationService, IPlaygroundMode playgroundMode)
    {
        _mediator = mediator;
        _translationService = translationService;
        _playgroundMode = playgroundMode;
    }

    public override async Task<SkillResult> ExecuteAsync(
        SkillExecutionContext context,
        Dictionary<string, object> parameters,
        CancellationToken cancellationToken = default)
    {
        if (_playgroundMode.IsActive)
        {
            return SkillResult.Error(PlaygroundHardeningConstants.FeatureBlockedMessage);
        }

        var emailId = GetRequiredGuid(parameters, "emailId");
        var targetLanguage = GetRequiredString(parameters, "targetLanguage").Trim().ToLowerInvariant();

        if (!LanguageConfig.SupportedLanguages.Contains(targetLanguage, StringComparer.OrdinalIgnoreCase))
        {
            return SkillResult.Error(string.Format(
                UnsupportedLanguageMessage, targetLanguage, string.Join(", ", LanguageConfig.SupportedLanguages)));
        }

        if (!await _translationService.IsConfiguredAsync())
        {
            return SkillResult.Error(NotConfiguredMessage);
        }

        TranslatedEmailResource? translated;
        try
        {
            translated = await _mediator.Send(new TranslateReceivedEmailQuery(emailId, targetLanguage), cancellationToken);
        }
        catch (TranslationAuthenticationException ex)
        {
            return SkillResult.Error(ex.Message);
        }

        if (translated == null)
        {
            return SkillResult.Error($"Email '{emailId}' not found.");
        }

        var body = EmailBodyPlainText.From(translated.BodyText, translated.BodyHtml);
        var truncated = body.Length > MaxBodyLength;
        if (truncated)
        {
            body = body[..MaxBodyLength] + TruncationMarker;
        }

        return SkillResult.SuccessResult(
            new
            {
                EmailId = emailId,
                TargetLanguage = targetLanguage,
                translated.Subject,
                BodyText = body,
                Truncated = truncated
            },
            $"Email translated to '{targetLanguage}'. Subject: {translated.Subject}");
    }
}
