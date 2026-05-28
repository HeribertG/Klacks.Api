// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Picks the top-N Klacksy welcome-suggestion i18n keys by aggregating scores from four signals:
/// the user's last 30 days of skill usage, the current frontend route, the user's roles,
/// and the local time (weekday/hour). Returns the highest-scoring keys, deduplicated.
/// </summary>

namespace Klacks.Api.Application.Services.Assistant;

using Klacks.Api.Application.Constants;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Domain.Constants;
using Klacks.Api.Domain.Interfaces.Assistant;

public sealed class SuggestionsRanker : ISuggestionsRanker
{
    private const int HistoryWindowDays = 30;
    private const int HistorySkillsConsidered = 8;
    private const double HistoryScorePerHit = 0.3;
    private const double HistoryScoreCap = 1.5;

    private const double RouteHitScore = 0.7;
    private const double RoleAdminBoost = 0.5;
    private const double RoleNonAdminFallback = 0.3;
    private const double TimeMatchScore = 0.4;
    private const double FallbackBaseScore = 0.05;

    private const int MondayWeekday = 1;
    private const int FridayWeekday = 5;
    private const int MorningCutoffHour = 12;
    private const int LateAfternoonHour = 15;
    private const int EveningHour = 17;

    private static readonly Dictionary<string, string> SkillToSuggestion = new(StringComparer.OrdinalIgnoreCase)
    {
        ["create_employee"] = WelcomeI18nKeys.Suggestion.CreateEmployee,
        ["create_client"] = WelcomeI18nKeys.Suggestion.CreateEmployee,
        ["search_clients"] = WelcomeI18nKeys.Suggestion.FindPerson,
        ["search_persons"] = WelcomeI18nKeys.Suggestion.FindPerson,
        ["search_employees"] = WelcomeI18nKeys.Suggestion.FindPerson,
        ["view_schedule"] = WelcomeI18nKeys.Suggestion.ViewSchedule,
        ["open_schedule"] = WelcomeI18nKeys.Suggestion.ViewSchedule,
        ["create_absence"] = WelcomeI18nKeys.Suggestion.ReviewAbsences,
        ["list_absences"] = WelcomeI18nKeys.Suggestion.ReviewAbsences,
        ["create_provider"] = WelcomeI18nKeys.Suggestion.ManageProviders,
        ["edit_provider"] = WelcomeI18nKeys.Suggestion.ManageProviders,
    };

    private static readonly (string RouteFragment, string[] SuggestionKeys)[] RouteHints =
    {
        ("/workplace/schedule", new[] { WelcomeI18nKeys.Suggestion.ViewSchedule, WelcomeI18nKeys.Suggestion.ReviewWeek }),
        ("/workplace/clients", new[] { WelcomeI18nKeys.Suggestion.FindPerson, WelcomeI18nKeys.Suggestion.CreateEmployee }),
        ("/workplace/absence", new[] { WelcomeI18nKeys.Suggestion.ReviewAbsences }),
        ("/workplace/settings/llm", new[] { WelcomeI18nKeys.Suggestion.ManageProviders }),
        ("/workplace/settings", new[] { WelcomeI18nKeys.Suggestion.EditSettings }),
    };

    private readonly ISkillUsageRepository _skillUsageRepository;

    public SuggestionsRanker(ISkillUsageRepository skillUsageRepository)
    {
        _skillUsageRepository = skillUsageRepository;
    }

    public async Task<IReadOnlyList<string>> RankAsync(
        Guid userId,
        IReadOnlyList<string> userRights,
        string? currentRoute,
        int localHour,
        int weekday,
        int topN,
        CancellationToken cancellationToken = default)
    {
        var scores = new Dictionary<string, double>(StringComparer.Ordinal);

        await ApplyHistoryScoresAsync(userId, scores, cancellationToken);
        ApplyRouteScores(currentRoute, scores);
        ApplyRoleScores(userRights, scores);
        ApplyTimeScores(localHour, weekday, scores);
        ApplyFallbackScores(scores);

        return scores
            .OrderByDescending(kv => kv.Value)
            .ThenBy(kv => kv.Key, StringComparer.Ordinal)
            .Take(topN)
            .Select(kv => kv.Key)
            .ToList();
    }

    private async Task ApplyHistoryScoresAsync(
        Guid userId,
        Dictionary<string, double> scores,
        CancellationToken cancellationToken)
    {
        if (userId == Guid.Empty)
        {
            return;
        }

        var since = DateTime.UtcNow.AddDays(-HistoryWindowDays);
        var records = await _skillUsageRepository.GetRecordsByUserAsync(userId, since, cancellationToken);

        var topGroups = records
            .GroupBy(r => r.SkillName ?? string.Empty, StringComparer.OrdinalIgnoreCase)
            .OrderByDescending(g => g.Count())
            .Take(HistorySkillsConsidered);

        foreach (var group in topGroups)
        {
            if (SkillToSuggestion.TryGetValue(group.Key, out var suggestionKey))
            {
                var bonus = Math.Min(group.Count() * HistoryScorePerHit, HistoryScoreCap);
                Bump(scores, suggestionKey, bonus);
            }
        }
    }

    private static void ApplyRouteScores(string? currentRoute, Dictionary<string, double> scores)
    {
        if (string.IsNullOrWhiteSpace(currentRoute))
        {
            return;
        }

        foreach (var (fragment, keys) in RouteHints)
        {
            if (currentRoute.Contains(fragment, StringComparison.OrdinalIgnoreCase))
            {
                foreach (var key in keys)
                {
                    Bump(scores, key, RouteHitScore);
                }
            }
        }
    }

    private static void ApplyRoleScores(IReadOnlyList<string> userRights, Dictionary<string, double> scores)
    {
        var isAdmin = userRights.Contains(Roles.Admin, StringComparer.OrdinalIgnoreCase);

        if (isAdmin)
        {
            Bump(scores, WelcomeI18nKeys.Suggestion.ManageProviders, RoleAdminBoost);
            Bump(scores, WelcomeI18nKeys.Suggestion.EditSettings, RoleAdminBoost);
        }
        else
        {
            Bump(scores, WelcomeI18nKeys.Suggestion.EditSettings, RoleNonAdminFallback);
        }
    }

    private static void ApplyTimeScores(int localHour, int weekday, Dictionary<string, double> scores)
    {
        if (weekday == MondayWeekday && localHour < MorningCutoffHour)
        {
            Bump(scores, WelcomeI18nKeys.Suggestion.ReviewWeek, TimeMatchScore);
        }

        if (weekday == FridayWeekday && localHour >= LateAfternoonHour)
        {
            Bump(scores, WelcomeI18nKeys.Suggestion.WeekendRecap, TimeMatchScore);
        }

        if (localHour >= EveningHour)
        {
            Bump(scores, WelcomeI18nKeys.Suggestion.EndOfDay, TimeMatchScore);
        }
    }

    private static void ApplyFallbackScores(Dictionary<string, double> scores)
    {
        Bump(scores, WelcomeI18nKeys.Suggestion.CreateEmployee, FallbackBaseScore + 0.4);
        Bump(scores, WelcomeI18nKeys.Suggestion.FindPerson, FallbackBaseScore + 0.3);
        Bump(scores, WelcomeI18nKeys.Suggestion.ShowHelp, FallbackBaseScore + 0.2);
    }

    private static void Bump(Dictionary<string, double> scores, string key, double delta)
    {
        scores[key] = scores.TryGetValue(key, out var existing) ? existing + delta : delta;
    }
}
