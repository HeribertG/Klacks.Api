// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Builds and caches the identity prompt (global rules + soul sections) for an agent.
/// Resolves template variables like {{LANGUAGE}} from configuration.
/// </summary>
/// <param name="soulRepository">Repository for agent soul sections</param>
/// <param name="globalRuleRepository">Repository for globally active agent rules</param>
/// <param name="configuration">App configuration for language metadata</param>
/// <param name="cache">In-memory cache for identity prompt caching</param>

using System.Text;
using System.Text.RegularExpressions;
using Klacks.Api.Domain.Interfaces.Assistant;
using Microsoft.Extensions.Caching.Memory;

namespace Klacks.Api.Domain.Services.Assistant;

public class IdentityContextProvider : IIdentityContextProvider
{
    private readonly IAgentSoulRepository _soulRepository;
    private readonly IGlobalAgentRuleRepository _globalRuleRepository;
    private readonly IConfiguration _configuration;
    private readonly IMemoryCache _cache;

    private const int IdentityCacheMinutes = 3;

    private static readonly Regex TemplateVariableRegex = new(
        @"\{\{(\w+)\}\}", RegexOptions.Compiled);

    public IdentityContextProvider(
        IAgentSoulRepository soulRepository,
        IGlobalAgentRuleRepository globalRuleRepository,
        IConfiguration configuration,
        IMemoryCache cache)
    {
        _soulRepository = soulRepository;
        _globalRuleRepository = globalRuleRepository;
        _configuration = configuration;
        _cache = cache;
    }

    public async Task<string> GetIdentityPromptAsync(
        Guid agentId, string? language, CancellationToken cancellationToken = default)
    {
        var cacheKey = $"identity_prompt_{agentId}_{language ?? "en"}";

        if (_cache.TryGetValue(cacheKey, out string? cached) && cached != null)
        {
            return cached;
        }

        var templateVariables = BuildTemplateVariables(language);
        var sb = new StringBuilder();

        var globalRules = await _globalRuleRepository.GetActiveRulesAsync(cancellationToken);
        if (globalRules.Count > 0)
        {
            sb.AppendLine("=== GLOBAL RULES ===");
            foreach (var rule in globalRules)
            {
                sb.AppendLine($"[{rule.Name.ToUpperInvariant()}]");
                sb.AppendLine(ResolveTemplateVariables(rule.Content.Trim(), templateVariables));
                sb.AppendLine();
            }
            sb.AppendLine("====================");
            sb.AppendLine();
        }

        var sections = await _soulRepository.GetActiveSectionsAsync(agentId, cancellationToken);
        if (sections.Count > 0)
        {
            sb.AppendLine("=== IDENTITY ===");
            foreach (var section in sections)
            {
                sb.AppendLine($"[{section.SectionType.ToUpperInvariant()}]");
                sb.AppendLine(section.Content.Trim());
                sb.AppendLine();
            }
            sb.AppendLine("================");
            sb.AppendLine();
        }

        var result = sb.ToString();

        _cache.Set(cacheKey, result, new MemoryCacheEntryOptions()
            .SetAbsoluteExpiration(TimeSpan.FromMinutes(IdentityCacheMinutes)));

        return result;
    }

    private Dictionary<string, string> BuildTemplateVariables(string? language)
    {
        var langCode = language ?? "en";
        var langName = _configuration[$"Languages:Metadata:{langCode}:Name"];
        var langDisplayName = _configuration[$"Languages:Metadata:{langCode}:DisplayName"];
        var langLabel = !string.IsNullOrEmpty(langName) && !string.IsNullOrEmpty(langDisplayName)
            ? $"{langName} ({langDisplayName})"
            : langCode;

        return new Dictionary<string, string>
        {
            ["LANGUAGE"] = langLabel,
            ["LANGUAGE_CODE"] = langCode,
        };
    }

    private static string ResolveTemplateVariables(
        string content, Dictionary<string, string> variables)
    {
        return TemplateVariableRegex.Replace(content, match =>
        {
            var key = match.Groups[1].Value;
            return variables.GetValueOrDefault(key, match.Value);
        });
    }
}
