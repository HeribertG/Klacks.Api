// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Translates a received email into a target language via the mailbox translation service
/// (the inbox "translate" button) and returns the translated subject and body text.
/// </summary>
/// <param name="emailId">Required. UUID of the email (from list_emails).</param>
/// <param name="targetLanguage">Required. ISO language code to translate into (e.g. de, en, fr, it).</param>

using Klacks.Api.Application.Queries.Email;
using Klacks.Api.Domain.Attributes;
using Klacks.Api.Domain.Models.Assistant;
using Klacks.Api.Domain.Services.Assistant.Skills.Implementations;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Skills;

[SkillImplementation("translate_email")]
public class TranslateEmailSkill : BaseSkillImplementation
{
    private const int MaxBodyLength = 4000;
    private const string TruncationMarker = " … [body truncated]";

    private readonly IMediator _mediator;

    public TranslateEmailSkill(IMediator mediator)
    {
        _mediator = mediator;
    }

    public override async Task<SkillResult> ExecuteAsync(
        SkillExecutionContext context,
        Dictionary<string, object> parameters,
        CancellationToken cancellationToken = default)
    {
        var emailId = GetRequiredGuid(parameters, "emailId");
        var targetLanguage = GetRequiredString(parameters, "targetLanguage").Trim().ToLowerInvariant();

        var translated = await _mediator.Send(
            new TranslateReceivedEmailQuery(emailId, targetLanguage), cancellationToken);
        if (translated == null)
        {
            return SkillResult.Error(
                $"Email '{emailId}' could not be translated — it may not exist or the translation service " +
                "is not configured (see get_deepl_settings).");
        }

        var body = translated.BodyText ?? string.Empty;
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
