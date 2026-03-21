// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Regelbasierte Sentiment-Analyse für eingehende Nutzernachrichten.
/// Erkennt Stimmungen (Frustrated, Stressed, Confused, Positive, Urgent, Neutral)
/// anhand von DB-gespeicherten mehrsprachigen Keyword-Listen und sprachunabhängigen Pattern-Regeln.
/// Singleton mit Lazy-Load-Cache; ReloadKeywords() nach Plugin-Install aufrufen.
/// </summary>

using System.Collections.Concurrent;
using System.Text.RegularExpressions;
using Klacks.Api.Domain.Constants;
using Klacks.Api.Domain.Interfaces.Assistant;
using Klacks.Api.Domain.Models.Assistant;

namespace Klacks.Api.Domain.Services.Assistant;

public class SentimentAnalyzer : ISentimentAnalyzer
{
    private static readonly string[] UniversalPositiveKeywords = ["👍", "😊", "🎉", "✅"];

    private static readonly Regex MultipleExclamationRegex = new(@"!{2,}", RegexOptions.Compiled);
    private static readonly Regex MultipleQuestionRegex = new(@"\?{2,}", RegexOptions.Compiled);
    private static readonly Regex AllCapsWordRegex = new(@"\b[A-ZÜÄÖ]{3,}\b", RegexOptions.Compiled);

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ConcurrentDictionary<string, List<string>> _keywordCache = new();
    private volatile bool _loaded;
    private readonly object _loadLock = new();

    public SentimentAnalyzer(IServiceScopeFactory scopeFactory)
    {
        _scopeFactory = scopeFactory;
    }

    public async Task<SentimentResult> AnalyzeSentimentAsync(string userMessage)
    {
        if (string.IsNullOrWhiteSpace(userMessage))
            return new SentimentResult(SentimentMood.Neutral, 0f);

        await EnsureLoadedAsync();

        var lower = userMessage.ToLowerInvariant();

        var allCapsCount = AllCapsWordRegex.Matches(userMessage).Count;
        var exclamationCount = MultipleExclamationRegex.Matches(userMessage).Count;
        var multiQuestionCount = MultipleQuestionRegex.Matches(userMessage).Count;

        var urgentScore = CountKeywordHits(lower, GetCategory(SentimentCategories.Urgent)) + allCapsCount;
        if (urgentScore >= 2)
            return new SentimentResult(SentimentMood.Urgent, CalculateConfidence(urgentScore, 3));

        var frustratedScore = CountKeywordHits(lower, GetCategory(SentimentCategories.Frustrated)) + exclamationCount;
        var stressedScore = CountKeywordHits(lower, GetCategory(SentimentCategories.Stressed));
        var confusedScore = CountKeywordHits(lower, GetCategory(SentimentCategories.Confused)) + multiQuestionCount;
        var positiveScore = CountKeywordHits(lower, GetCategory(SentimentCategories.Positive))
            + CountKeywordHits(userMessage, UniversalPositiveKeywords);

        var scores = new[]
        {
            (SentimentMood.Frustrated, frustratedScore),
            (SentimentMood.Stressed, stressedScore),
            (SentimentMood.Confused, confusedScore),
            (SentimentMood.Positive, positiveScore)
        };

        var best = scores.OrderByDescending(s => s.Item2).First();

        if (best.Item2 == 0)
            return new SentimentResult(SentimentMood.Neutral, 1f);

        return new SentimentResult(best.Item1, CalculateConfidence(best.Item2, 4));
    }

    public void ReloadKeywords()
    {
        lock (_loadLock)
        {
            _keywordCache.Clear();
            _loaded = false;
        }
    }

    private async Task EnsureLoadedAsync()
    {
        if (_loaded)
            return;

        await LoadKeywordsFromDatabaseAsync();
        _loaded = true;
    }

    private async Task LoadKeywordsFromDatabaseAsync()
    {
        using var scope = _scopeFactory.CreateScope();
        var repository = scope.ServiceProvider.GetRequiredService<ISentimentKeywordRepository>();

        var allSets = await repository.GetAllAsync();

        var merged = new Dictionary<string, List<string>>(StringComparer.Ordinal);

        foreach (var set in allSets)
        {
            foreach (var (category, keywords) in set.Keywords)
            {
                if (!merged.ContainsKey(category))
                    merged[category] = new List<string>();

                foreach (var keyword in keywords)
                {
                    if (!merged[category].Contains(keyword, StringComparer.OrdinalIgnoreCase))
                        merged[category].Add(keyword);
                }
            }
        }

        foreach (var (category, keywords) in merged)
        {
            _keywordCache[category] = keywords;
        }
    }

    private List<string> GetCategory(string category)
    {
        return _keywordCache.TryGetValue(category, out var keywords)
            ? keywords
            : [];
    }

    private static int CountKeywordHits(string message, IEnumerable<string> keywords)
    {
        var count = 0;
        foreach (var keyword in keywords)
        {
            if (message.Contains(keyword, StringComparison.Ordinal))
                count++;
        }
        return count;
    }

    private static float CalculateConfidence(int hits, int saturationAt)
    {
        return Math.Min(1f, (float)hits / saturationAt);
    }
}
