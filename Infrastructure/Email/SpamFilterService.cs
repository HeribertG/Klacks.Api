// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Application.Constants;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Domain.Enums;
using Klacks.Api.Domain.Interfaces.Assistant;
using Klacks.Api.Domain.Interfaces.Email;
using Klacks.Api.Domain.Models.Assistant;
using Klacks.Api.Domain.Models.Email;

namespace Klacks.Api.Infrastructure.Email;

public class SpamFilterService : ISpamFilterService
{
    private readonly ISpamRuleRepository _spamRuleRepository;
    private readonly ISettingsRepository _settingsRepository;
    private readonly ILLMService _llmService;
    private readonly ILogger<SpamFilterService> _logger;

    public SpamFilterService(
        ISpamRuleRepository spamRuleRepository,
        ISettingsRepository settingsRepository,
        ILLMService llmService,
        ILogger<SpamFilterService> logger)
    {
        _spamRuleRepository = spamRuleRepository;
        _settingsRepository = settingsRepository;
        _llmService = llmService;
        _logger = logger;
    }

    public async Task<SpamFilterResult> ClassifyAsync(ReceivedEmail email, CancellationToken cancellationToken = default)
    {
        var ruleResult = await EvaluateRulesAsync(email);

        var spamThreshold = await GetSettingFloat(Settings.SPAM_FILTER_SPAM_THRESHOLD, SpamFilterDefaults.SpamThreshold);
        var uncertainThreshold = await GetSettingFloat(Settings.SPAM_FILTER_UNCERTAIN_THRESHOLD, SpamFilterDefaults.UncertainThreshold);

        if (ruleResult.Score >= spamThreshold)
        {
            ruleResult.IsSpam = true;
            return ruleResult;
        }

        if (ruleResult.Score < uncertainThreshold)
        {
            ruleResult.IsSpam = false;
            return ruleResult;
        }

        var llmEnabled = await GetSettingBool(Settings.SPAM_FILTER_LLM_ENABLED, false);
        if (!llmEnabled)
        {
            ruleResult.IsSpam = false;
            return ruleResult;
        }

        return await ClassifyWithLlmAsync(email, ruleResult);
    }

    private async Task<SpamFilterResult> EvaluateRulesAsync(ReceivedEmail email)
    {
        var rules = await _spamRuleRepository.GetAllActiveAsync();

        foreach (var rule in rules)
        {
            if (MatchesRule(email, rule))
            {
                return new SpamFilterResult
                {
                    Score = 1.0f,
                    IsSpam = true,
                    Reason = $"Matched rule: {rule.RuleType} with pattern '{rule.Pattern}'",
                    UsedLlm = false
                };
            }
        }

        return new SpamFilterResult
        {
            Score = 0.0f,
            IsSpam = false,
            Reason = "No rule matched",
            UsedLlm = false
        };
    }

    private static bool MatchesRule(ReceivedEmail email, SpamRule rule)
    {
        return rule.RuleType switch
        {
            SpamRuleType.SenderContains =>
                ContainsIgnoreCase(email.FromAddress, rule.Pattern) ||
                ContainsIgnoreCase(email.FromName, rule.Pattern),

            SpamRuleType.SenderDomain =>
                MatchesDomain(email.FromAddress, rule.Pattern),

            SpamRuleType.SubjectContains =>
                ContainsIgnoreCase(email.Subject, rule.Pattern),

            SpamRuleType.BodyContains =>
                ContainsIgnoreCase(email.BodyText, rule.Pattern) ||
                ContainsIgnoreCase(email.BodyHtml, rule.Pattern),

            _ => false
        };
    }

    private static bool ContainsIgnoreCase(string? text, string pattern)
    {
        if (string.IsNullOrEmpty(text) || string.IsNullOrEmpty(pattern))
            return false;

        return text.Contains(pattern, StringComparison.OrdinalIgnoreCase);
    }

    private static bool MatchesDomain(string? emailAddress, string pattern)
    {
        if (string.IsNullOrEmpty(emailAddress) || string.IsNullOrEmpty(pattern))
            return false;

        var atIndex = emailAddress.IndexOf('@');
        if (atIndex < 0 || atIndex >= emailAddress.Length - 1)
            return false;

        var domain = emailAddress[(atIndex + 1)..];
        return string.Equals(domain, pattern, StringComparison.OrdinalIgnoreCase);
    }

    private async Task<SpamFilterResult> ClassifyWithLlmAsync(ReceivedEmail email, SpamFilterResult ruleResult)
    {
        try
        {
            var bodyExcerpt = TruncateBody(email.BodyText ?? email.BodyHtml ?? string.Empty);

            var context = new LLMContext
            {
                Message = $"Classify this email as SPAM or HAM. Reply with only one word: SPAM or HAM.\n\nFrom: {email.FromAddress}\nSubject: {email.Subject}\nBody: {bodyExcerpt}",
                ModelId = Settings.LLM_FALLBACK_MODEL_ID
            };

            var response = await _llmService.ProcessAsync(context);

            if (response.Message.Contains("SPAM", StringComparison.OrdinalIgnoreCase))
            {
                return new SpamFilterResult
                {
                    Score = 0.9f,
                    IsSpam = true,
                    Reason = "LLM classified as SPAM",
                    UsedLlm = true
                };
            }

            return new SpamFilterResult
            {
                Score = 0.1f,
                IsSpam = false,
                Reason = "LLM classified as HAM",
                UsedLlm = true
            };
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "LLM classification failed, falling back to rule-based score");
            ruleResult.Reason += " (LLM fallback failed)";
            return ruleResult;
        }
    }

    private static string TruncateBody(string body)
    {
        if (body.Length <= SpamFilterDefaults.MaxBodyLengthForLlm)
            return body;

        return body[..SpamFilterDefaults.MaxBodyLengthForLlm];
    }

    private async Task<float> GetSettingFloat(string key, float defaultValue)
    {
        var setting = await _settingsRepository.GetSetting(key);
        if (setting?.Value != null && float.TryParse(setting.Value, System.Globalization.CultureInfo.InvariantCulture, out var value))
            return value;

        return defaultValue;
    }

    private async Task<bool> GetSettingBool(string key, bool defaultValue)
    {
        var setting = await _settingsRepository.GetSetting(key);
        if (setting?.Value != null && bool.TryParse(setting.Value, out var value))
            return value;

        return defaultValue;
    }
}
