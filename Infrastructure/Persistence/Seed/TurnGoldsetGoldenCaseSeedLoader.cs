// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Imports the shipped turn-selection goldset into the golden-case table so the regression gate has
/// something to replay from the first startup on. Idempotent, keyed on origin, query and locale: a case
/// the loop later froze by itself is never touched, and an unchanged restart writes nothing.
/// It is an upsert, not an insert: when the shipped file corrects the expected tool of a query that was
/// already imported, the existing row is updated. Insert-only left the gate measuring against the answer
/// that had been found wrong, with no way to correct it short of deleting rows by hand.
/// Nothing is ever pruned. An edited message is a different key, so it becomes a new case while the row
/// of the old wording stays - the seeder cannot tell an edited goldset entry from a case somebody meant
/// to keep. Duplicate keys already in the table are left as they are; the first row of a key wins.
/// Every item that names an expected tool is imported, including items whose tool no longer exists in
/// this installation. Filtering by the live catalogue would make the population machine-dependent, and
/// the consumers that recompute the partition from the item id would then disagree with the rows. A case
/// whose tool is gone simply fails on every run and is subtracted out again by the gate's baseline.
/// The query is capped at the excerpt limit the table enforces; the two goldset messages longer than
/// that are stored truncated, and the idempotency key uses the truncated text because that is what a
/// later run reads back.
/// </summary>
/// <param name="goldsetLoader">Reads the shipped goldset file</param>
/// <param name="goldenCaseRepository">Existing cases of the goldset origin, and the bulk insert</param>
/// <param name="logger">One line per startup with inserted, existing and truncated counts</param>

using Klacks.Api.Application.Services.Assistant.Evaluation.TurnEval;
using Klacks.Api.Domain.Constants;
using Klacks.Api.Domain.Interfaces.Assistant;
using Klacks.Api.Domain.Models.Assistant;
using Klacks.Api.Domain.Services.Assistant;

namespace Klacks.Api.Infrastructure.Persistence.Seed;

public class TurnGoldsetGoldenCaseSeedLoader
{
    private readonly ITurnGoldsetLoader _goldsetLoader;
    private readonly ISkillLearningGoldenCaseRepository _goldenCaseRepository;
    private readonly ILogger<TurnGoldsetGoldenCaseSeedLoader> _logger;

    public TurnGoldsetGoldenCaseSeedLoader(
        ITurnGoldsetLoader goldsetLoader,
        ISkillLearningGoldenCaseRepository goldenCaseRepository,
        ILogger<TurnGoldsetGoldenCaseSeedLoader> logger)
    {
        _goldsetLoader = goldsetLoader;
        _goldenCaseRepository = goldenCaseRepository;
        _logger = logger;
    }

    public async Task LoadAsync(CancellationToken cancellationToken = default)
    {
        IReadOnlyList<TurnGoldsetItem> items;
        try
        {
            items = await _goldsetLoader.LoadAsync(TurnEvalDefaults.DefaultGoldset, cancellationToken);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            _logger.LogWarning(
                exception, "Goldset '{Goldset}' could not be read; no golden cases were seeded", TurnEvalDefaults.DefaultGoldset);
            return;
        }

        var existing = await _goldenCaseRepository.ListByOriginAsync(
            GoldenCaseOrigins.Goldset, cancellationToken);

        var storedByKey = new Dictionary<(string Query, string Locale), SkillLearningGoldenCase>();
        foreach (var goldenCase in existing)
        {
            storedByKey.TryAdd((goldenCase.Query, goldenCase.Locale), goldenCase);
        }

        var seen = new HashSet<(string Query, string Locale)>();
        var toAdd = new List<SkillLearningGoldenCase>();
        var toUpdate = new List<SkillLearningGoldenCase>();
        var truncated = 0;

        foreach (var item in items)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (string.IsNullOrWhiteSpace(item.ExpectedTool) || string.IsNullOrWhiteSpace(item.Message))
            {
                continue;
            }

            var query = item.Message.Trim();
            if (query.Length > SkillLearningDefaults.ExcerptMaxLength)
            {
                query = query[..SkillLearningDefaults.ExcerptMaxLength];
                truncated++;
            }

            var key = (Query: query, Locale: item.Locale ?? string.Empty);
            if (!seen.Add(key))
            {
                continue;
            }

            if (storedByKey.TryGetValue(key, out var stored))
            {
                if (!string.Equals(stored.ExpectedSourceId, item.ExpectedTool, StringComparison.Ordinal))
                {
                    stored.ExpectedSourceId = item.ExpectedTool;
                    toUpdate.Add(stored);
                }

                continue;
            }

            toAdd.Add(new SkillLearningGoldenCase
            {
                Id = Guid.NewGuid(),
                Query = key.Query,
                Locale = key.Locale,
                ExpectedSourceId = item.ExpectedTool,
                Origin = GoldenCaseOrigins.Goldset,
                Partition = GoldsetPartitioner.Resolve(item.Id)
            });
        }

        if (toAdd.Count > 0)
        {
            await _goldenCaseRepository.AddRangeAsync(toAdd, cancellationToken);
        }

        if (toUpdate.Count > 0)
        {
            await _goldenCaseRepository.UpdateRangeAsync(toUpdate, cancellationToken);
        }

        _logger.LogInformation(
            "Goldset golden cases seeded from '{Goldset}': {Added} added, {Updated} corrected, "
                + "{Existing} already present, {Truncated} query/queries truncated to {Limit} characters",
            TurnEvalDefaults.DefaultGoldset, toAdd.Count, toUpdate.Count, existing.Count, truncated,
            SkillLearningDefaults.ExcerptMaxLength);
    }
}
