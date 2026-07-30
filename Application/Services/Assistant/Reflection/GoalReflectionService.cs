// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Self-directed goal reflection: bundles each user's recent GoalSignal set into a single cheap LLM
/// call and drafts up to MaxCandidatesPerUser goal candidates. The LLM does not write the proposal
/// text — it only selects goal types from GoalTypeCatalog and rates its own confidence, because the
/// server cannot know the recipient's UI language (the same reason ProactiveMessageI18nKeys exists)
/// and free prose would also carry internal identifiers into what a user reads. What is persisted is
/// therefore the selected goal type plus the interpolation values for the catalogue's text; the
/// frontend renders title and rationale from the catalogue's i18n keys. Title and Rationale hold the
/// catalogue's canonical English wording for the planning agent and the audit log only. See
/// docs/superpowers/specs/2026-07-28-klacksy-selbstgesteuerte-ziele-design.md, phases P1/P2. While
/// BackgroundServiceOptions.GoalReflectionDelivery is off (Phase 1 shadow mode), every candidate is
/// persisted with Status = Shadow and nothing is reachable through the goal-candidates inbox. Once on
/// (Phase 2), candidates are persisted with Status = Proposed, but only for users the
/// IPlanningAudienceResolver reports as planners (Admin/Authorised) — signals from any other user are
/// skipped and counted rather than turned into a visible candidate. No plan is ever drafted and no
/// skill is ever executed here regardless of the flag. A failure for one user is logged and never
/// aborts the cycle for the remaining users.
/// </summary>
/// <param name="signalSource">Collects the raw signals the reflection LLM reasons over.</param>
/// <param name="goalCandidateRepository">Persists candidates and checks for recent duplicates.</param>
/// <param name="cheapestModelResolver">Resolves the cheapest enabled LLM model and provider, same as PlanningAgent.</param>
/// <param name="planningAudienceResolver">Resolves which users are planners, to gate Phase 2 delivery.</param>
/// <param name="options">Feature flag deciding shadow (Status = Shadow) vs. delivery (Status = Proposed, planners only).</param>
/// <param name="logger">Structured log of created/skipped/discarded candidates per cycle.</param>

using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Klacks.Api.Application.Configuration;
using Klacks.Api.Domain.Constants;
using Klacks.Api.Domain.Interfaces.Assistant;
using Klacks.Api.Domain.Logging;
using Klacks.Api.Domain.Models.Assistant;
using Klacks.Api.Domain.Services.Assistant.Providers;
using Microsoft.Extensions.Options;

namespace Klacks.Api.Application.Services.Assistant.Reflection;

public class GoalReflectionService : IGoalReflectionService
{
    private const int MaxCandidatesPerUser = 3;
    private const int DedupWindowDays = 14;
    private const int MaxOutputTokens = 512;
    private const double Temperature = 0.2;

    // Must not exceed the column lengths GoalCandidateConfiguration declares, so a catalogue entry that
    // grows too long never fails persistence with a truncation error.
    private const int MaxTitleLength = 256;
    private const int MaxRationaleLength = 2048;

    private static readonly string SystemPrompt =
        "You are Klacksy's self-reflection module. You receive recurring observations made for one user, " +
        "each with a goalType identifier and a plain-language description, and decide which of them are " +
        "worth proposing as a goal. " +
        "Rules:\n" +
        "1. Propose at most " + MaxCandidatesPerUser + " candidates, and only goalType values that appear " +
        "   in the input. If nothing is worth proposing, return an empty array.\n" +
        "2. Never propose the same goalType twice.\n" +
        "3. Each candidate needs the goalType exactly as given plus a 'confidence' field that is exactly " +
        "   'high' or 'low' — 'high' only when the observation is unambiguous and recurring, 'low' for " +
        "   anything speculative.\n" +
        "4. Write no titles, no rationales, no skill names and no execution steps: the wording shown to " +
        "   the user comes from a catalogue, you only select and rate.\n\n" +
        "Respond ONLY with JSON of shape: " +
        "{\"candidates\":[{\"goalType\":\"...\",\"confidence\":\"high|low\"}]}";

    private readonly IGoalSignalSource _signalSource;
    private readonly IGoalCandidateRepository _goalCandidateRepository;
    private readonly ICheapestModelResolver _cheapestModelResolver;
    private readonly IPlanningAudienceResolver _planningAudienceResolver;
    private readonly BackgroundServiceOptions _options;
    private readonly ILogger<GoalReflectionService> _logger;

    public GoalReflectionService(
        IGoalSignalSource signalSource,
        IGoalCandidateRepository goalCandidateRepository,
        ICheapestModelResolver cheapestModelResolver,
        IPlanningAudienceResolver planningAudienceResolver,
        IOptions<BackgroundServiceOptions> options,
        ILogger<GoalReflectionService> logger)
    {
        _signalSource = signalSource;
        _goalCandidateRepository = goalCandidateRepository;
        _cheapestModelResolver = cheapestModelResolver;
        _planningAudienceResolver = planningAudienceResolver;
        _options = options.Value;
        _logger = logger;
    }

    public async Task<int> RunReflectionCycleAsync(CancellationToken cancellationToken = default)
    {
        var signals = await _signalSource.CollectAsync(cancellationToken);
        if (signals.Count == 0)
        {
            _logger.LogDebug("Goal reflection cycle: no signals collected, nothing to do");
            return 0;
        }

        if (_options.GoalReflectionDelivery)
        {
            var plannerIds = await _planningAudienceResolver.GetPlanningUserIdsAsync(cancellationToken);
            var filteredSignals = signals.Where(s => plannerIds.Contains(s.UserId)).ToList();
            var skippedNonPlannerCount = signals.Count - filteredSignals.Count;
            if (skippedNonPlannerCount > 0)
            {
                _logger.LogInformation(
                    "Goal reflection cycle: skipped {Count} signal(s) from non-planner user(s) — delivery is planners-only",
                    skippedNonPlannerCount);
            }

            signals = filteredSignals;
            if (signals.Count == 0)
            {
                _logger.LogDebug("Goal reflection cycle: no planner signals remain after audience gating, nothing to do");
                return 0;
            }
        }

        var (model, provider) = await _cheapestModelResolver.ResolveAsync(cancellationToken);
        if (model == null || provider == null)
        {
            _logger.LogWarning("Goal reflection cycle skipped — no enabled LLM model/provider available");
            return 0;
        }

        var persisted = 0;
        var skippedAsDuplicate = 0;
        var discarded = 0;

        foreach (var group in signals.GroupBy(s => s.UserId))
        {
            var userId = group.Key;
            try
            {
                var (userPersisted, userSkipped, userDiscarded) = await ReflectForUserAsync(
                    userId, group.ToList(), model, provider, cancellationToken);
                persisted += userPersisted;
                skippedAsDuplicate += userSkipped;
                discarded += userDiscarded;
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Goal reflection failed for user {UserId} — other users continue", userId.ForLog());
            }
        }

        _logger.LogInformation(
            "Goal reflection cycle complete — {Persisted} candidate(s) created, {Skipped} skipped as duplicate, {Discarded} discarded",
            persisted, skippedAsDuplicate, discarded);

        return persisted;
    }

    private async Task<(int Persisted, int SkippedAsDuplicate, int Discarded)> ReflectForUserAsync(
        string userId,
        IReadOnlyList<GoalSignal> userSignals,
        LLMModel model,
        ILLMProvider provider,
        CancellationToken cancellationToken)
    {
        var request = new LLMProviderRequest
        {
            Message = RenderSignalPrompt(userSignals),
            SystemPrompt = SystemPrompt,
            ModelId = model.ApiModelId,
            ConversationHistory = [],
            AvailableFunctions = [],
            Temperature = Temperature,
            MaxTokens = MaxOutputTokens,
            SupportedParameters = model.SupportedParameters,
            CostPerInputToken = model.CostPerInputToken,
            CostPerOutputToken = model.CostPerOutputToken
        };

        var response = await provider.ProcessAsync(request, cancellationToken);
        if (!response.Success || string.IsNullOrWhiteSpace(response.Content))
        {
            _logger.LogDebug("Goal reflection LLM call returned no content for user {UserId}", userId.ForLog());
            return (0, 0, 0);
        }

        var signalsByGoalType = userSignals
            .GroupBy(s => s.Kind, StringComparer.Ordinal)
            .ToDictionary(g => g.Key, g => g.First(), StringComparer.Ordinal);

        var candidates = ParseCandidates(response.Content);

        var persisted = 0;
        var skippedAsDuplicate = 0;
        var discarded = 0;
        var proposedGoalTypes = new HashSet<string>(StringComparer.Ordinal);

        foreach (var candidate in candidates)
        {
            // A goalType the user has no signal for is a hallucination, and the same type twice in one
            // response is the model repeating itself — both are dropped rather than persisted.
            if (!signalsByGoalType.TryGetValue(candidate.GoalType, out var signal) ||
                !proposedGoalTypes.Add(candidate.GoalType))
            {
                discarded++;
                continue;
            }

            var definition = GoalTypeCatalog.Find(candidate.GoalType);
            if (definition == null)
            {
                discarded++;
                continue;
            }

            var dedupHash = ComputeDedupHash(candidate.GoalType, userId);
            var sinceUtc = DateTime.UtcNow.AddDays(-DedupWindowDays);

            var isDuplicate = await _goalCandidateRepository.ExistsRecentAsync(userId, dedupHash, sinceUtc, cancellationToken);
            if (isDuplicate)
            {
                skippedAsDuplicate++;
                continue;
            }

            var goalCandidate = new GoalCandidate
            {
                UserId = userId,
                GoalType = candidate.GoalType,
                RationaleParamsJson = BuildRationaleParamsJson(signal),
                Title = Truncate(definition.PlannerTitle, MaxTitleLength),
                Rationale = Truncate(BuildPlannerRationale(definition, signal), MaxRationaleLength),
                Status = _options.GoalReflectionDelivery ? GoalCandidateStatus.Proposed : GoalCandidateStatus.Shadow,
                Confidence = candidate.Confidence,
                SignalSource = signal.Kind,
                DedupHash = dedupHash,
                OwnerPermissionsCsv = null
            };

            await _goalCandidateRepository.AddAsync(goalCandidate, cancellationToken);
            persisted++;
        }

        return (persisted, skippedAsDuplicate, discarded);
    }

    private static string RenderSignalPrompt(IReadOnlyList<GoalSignal> signals)
    {
        var sb = new StringBuilder();
        sb.AppendLine("Recurring observations for this user (goalType — description):");
        foreach (var signal in signals)
        {
            sb.Append("- ").Append(signal.Kind).Append(" — ").AppendLine(signal.Summary);
        }
        return sb.ToString();
    }

    private static string BuildRationaleParamsJson(GoalSignal signal) =>
        JsonSerializer.Serialize(new Dictionary<string, string>
        {
            [GoalCandidateRationaleParams.Count] = signal.OccurrenceCount.ToString(CultureInfo.InvariantCulture),
            [GoalCandidateRationaleParams.Days] = signal.LookbackDays.ToString(CultureInfo.InvariantCulture)
        });

    private static string BuildPlannerRationale(GoalTypeDefinition definition, GoalSignal signal) =>
        string.Format(
            CultureInfo.InvariantCulture,
            definition.PlannerRationaleFormat,
            signal.OccurrenceCount,
            signal.LookbackDays);

    private List<ParsedCandidate> ParseCandidates(string content)
    {
        var result = new List<ParsedCandidate>();
        try
        {
            var json = ExtractJsonObject(content);
            if (string.IsNullOrWhiteSpace(json))
            {
                return result;
            }

            using var doc = JsonDocument.Parse(json);
            if (!doc.RootElement.TryGetProperty("candidates", out var candidatesElement) ||
                candidatesElement.ValueKind != JsonValueKind.Array)
            {
                return result;
            }

            foreach (var el in candidatesElement.EnumerateArray().Take(MaxCandidatesPerUser))
            {
                var goalType = el.TryGetProperty("goalType", out var goalTypeEl) && goalTypeEl.ValueKind == JsonValueKind.String
                    ? goalTypeEl.GetString()
                    : null;
                if (string.IsNullOrWhiteSpace(goalType))
                {
                    continue;
                }

                // Confidence gate (design spec): the model's literal 'low' is kept as Low, but anything
                // ambiguous — missing, empty, unparsable field, or any value that is neither 'high' nor
                // 'low' — becomes Unknown, the safe default. Phase 1 persists this purely to measure
                // whether 'high' correlates with quality; it is NOT used to filter candidates here, so
                // do not remove this branch as "dead" once Unknown looks unused.
                var confidenceRaw = el.TryGetProperty("confidence", out var confidenceEl) && confidenceEl.ValueKind == JsonValueKind.String
                    ? confidenceEl.GetString()
                    : null;
                var confidence = confidenceRaw switch
                {
                    _ when string.Equals(confidenceRaw, GoalCandidateConfidence.High, StringComparison.OrdinalIgnoreCase) => GoalCandidateConfidence.High,
                    _ when string.Equals(confidenceRaw, GoalCandidateConfidence.Low, StringComparison.OrdinalIgnoreCase) => GoalCandidateConfidence.Low,
                    _ => GoalCandidateConfidence.Unknown
                };

                result.Add(new ParsedCandidate(goalType!.Trim(), confidence));
            }
        }
        catch (JsonException ex)
        {
            _logger.LogDebug(ex, "Goal reflection JSON parse failed; treating cycle as producing no candidates");
        }
        return result;
    }

    private static string ExtractJsonObject(string content)
    {
        var start = content.IndexOf('{');
        if (start < 0) return string.Empty;
        var depth = 0;
        for (var i = start; i < content.Length; i++)
        {
            if (content[i] == '{') depth++;
            else if (content[i] == '}')
            {
                depth--;
                if (depth == 0) return content[start..(i + 1)];
            }
        }
        return string.Empty;
    }

    private static string Truncate(string value, int maxLength) =>
        value.Length > maxLength ? value[..maxLength] : value;

    // Hashed over the goal type rather than the proposal text: two cycles that pick the same type are
    // the same proposal, however the wording is rendered later.
    private static string ComputeDedupHash(string goalType, string userId)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(goalType + "|" + userId));
        return Convert.ToHexString(bytes);
    }

    private sealed record ParsedCandidate(string GoalType, string Confidence);
}
