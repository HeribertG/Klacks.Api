// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Composes the localized planner notices and the neutral reply subject of the inbound clarification
/// dialog. The language is the installation language (DEFAULT_LANGUAGE), resolved when a text is built and
/// not before, so a message that needs no text costs no settings read. The catalogue supplies the template,
/// ClarificationTextTemplate fills it in one pass, so employee text inside a value is never expanded twice.
/// An installed language whose pack lacks a key falls back to English with a warning in the log - never
/// silently, and never by dropping a planner notice; the pack coverage guard keeps that case from shipping.
/// The unclear-answer notice is appended to the answer context with a line break, a language-neutral join.
/// Time format and the shortening of the original text are the same in every language, so they stay here.
/// Skill outputs for the language model are not built here and stay English.
/// </summary>
/// <param name="languageResolver">Resolves the installation language</param>
/// <param name="logger">Logs a missing catalogue key</param>

using System.Globalization;
using Klacks.Api.Domain.Constants;
using Klacks.Api.Domain.Interfaces.Inbound;
using Klacks.Api.Domain.Interfaces.Settings;
using Klacks.Api.Domain.Models.Inbound;
using Klacks.Api.Domain.Services.Inbound;

namespace Klacks.Api.Infrastructure.Inbound;

public sealed class ClarificationTextService : IClarificationTextService
{
    private const string LocalTimeFormat = "yyyy-MM-dd HH:mm";
    private const string Ellipsis = "…";
    private const string NoticeSeparator = "\n";

    private readonly IInstallationLanguageResolver _languageResolver;
    private readonly ILogger<ClarificationTextService> _logger;

    public ClarificationTextService(IInstallationLanguageResolver languageResolver, ILogger<ClarificationTextService> logger)
    {
        _languageResolver = languageResolver;
        _logger = logger;
    }

    public async Task<string> StartedAsync(
        string sender, string summary, string question, string? shiftContext, DateTime deadlineLocal,
        CancellationToken cancellationToken = default)
    {
        var language = await _languageResolver.ResolveAsync(cancellationToken);
        return Render(
            language,
            ClarificationTextKeys.PlannerStarted,
            (ClarificationTextPlaceholders.Sender, sender),
            (ClarificationTextPlaceholders.Summary, summary),
            (ClarificationTextPlaceholders.Question, question),
            (ClarificationTextPlaceholders.ShiftContext, ShiftOrNone(language, shiftContext)),
            (ClarificationTextPlaceholders.Deadline, Format(deadlineLocal)));
    }

    public async Task<string> AnswerContextAsync(
        string question, DateTime askedLocal, string originalText, bool unresolved,
        CancellationToken cancellationToken = default)
    {
        var language = await _languageResolver.ResolveAsync(cancellationToken);
        var context = Render(
            language,
            ClarificationTextKeys.PlannerAnswerContext,
            (ClarificationTextPlaceholders.Question, question),
            (ClarificationTextPlaceholders.Asked, Format(askedLocal)),
            (ClarificationTextPlaceholders.OriginalText, Shorten(originalText)));

        return unresolved
            ? context + NoticeSeparator + Resolve(language, ClarificationTextKeys.PlannerAnswerUnclearNotice)
            : context;
    }

    public async Task<string> ExpiredAsync(
        string sender, string question, DateTime askedLocal, DateTime deadlineLocal, string originalText, string? shiftContext,
        CancellationToken cancellationToken = default)
    {
        var language = await _languageResolver.ResolveAsync(cancellationToken);
        return Render(
            language,
            ClarificationTextKeys.PlannerExpired,
            (ClarificationTextPlaceholders.Sender, sender),
            (ClarificationTextPlaceholders.Question, question),
            (ClarificationTextPlaceholders.Asked, Format(askedLocal)),
            (ClarificationTextPlaceholders.Deadline, Format(deadlineLocal)),
            (ClarificationTextPlaceholders.OriginalText, Shorten(originalText)),
            (ClarificationTextPlaceholders.ShiftContext, ShiftOrNone(language, shiftContext)));
    }

    public async Task<string> SendFailedAsync(string question, CancellationToken cancellationToken = default) =>
        Render(
            await _languageResolver.ResolveAsync(cancellationToken),
            ClarificationTextKeys.PlannerSendFailed,
            (ClarificationTextPlaceholders.Question, question));

    public async Task<string> SuggestedAsync(string question, CancellationToken cancellationToken = default) =>
        Render(
            await _languageResolver.ResolveAsync(cancellationToken),
            ClarificationTextKeys.PlannerSuggested,
            (ClarificationTextPlaceholders.Question, question));

    public async Task<string> NoPersonalTargetAsync(CancellationToken cancellationToken = default) =>
        Render(await _languageResolver.ResolveAsync(cancellationToken), ClarificationTextKeys.PlannerNoPersonalTarget);

    public async Task<string> AnsweredAfterExpiryAsync(
        string question, DateTime askedLocal, CancellationToken cancellationToken = default) =>
        Render(
            await _languageResolver.ResolveAsync(cancellationToken),
            ClarificationTextKeys.PlannerAnsweredAfterExpiry,
            (ClarificationTextPlaceholders.Question, question),
            (ClarificationTextPlaceholders.Asked, Format(askedLocal)));

    public async Task<string> ArrivedAfterClosureAsync(
        string question, DateTime askedLocal, InboundClarification current, CancellationToken cancellationToken = default)
    {
        var language = await _languageResolver.ResolveAsync(cancellationToken);
        return Render(
            language,
            ClarificationTextKeys.PlannerArrivedAfterClosure,
            (ClarificationTextPlaceholders.Question, question),
            (ClarificationTextPlaceholders.Asked, Format(askedLocal)),
            (ClarificationTextPlaceholders.Status, Resolve(language, ClarificationStatusText.KeyOf(current))));
    }

    public async Task<string> NeutralReplySubjectAsync(CancellationToken cancellationToken = default) =>
        Render(await _languageResolver.ResolveAsync(cancellationToken), ClarificationTextKeys.MailNeutralReplySubject);

    private string ShiftOrNone(string language, string? shiftContext) =>
        shiftContext ?? Resolve(language, ClarificationTextKeys.PlannerShiftNone);

    private string Render(string language, string key, params (string Name, string Value)[] values) =>
        ClarificationTextTemplate.Render(
            Resolve(language, key), values.ToDictionary(pair => pair.Name, pair => pair.Value, StringComparer.Ordinal));

    private string Resolve(string language, string key)
    {
        if (ClarificationTexts.TryGetText(key, language, out var text))
        {
            return text;
        }

        _logger.LogWarning(
            "The language pack of {Language} has no clarification text {Key}; using the English text", language, key);
        return ClarificationTexts.English(key);
    }

    private static string Format(DateTime local) => local.ToString(LocalTimeFormat, CultureInfo.InvariantCulture);

    private static string Shorten(string text) =>
        text.Length <= InboundClarificationConstants.MaxNotifiedOriginalTextLength
            ? text
            : text[..InboundClarificationConstants.MaxNotifiedOriginalTextLength] + Ellipsis;
}
