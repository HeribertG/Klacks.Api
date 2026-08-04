// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using System.Security.Cryptography;
using System.Text;
using Klacks.Api.KnowledgeIndex.Application.Constants;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Klacks.Api.KnowledgeIndex.Application.Services;

/// <summary>
/// Runs an action-keyed toolset cache in shadow mode: it looks up, compares and records, but never
/// influences what the turn does. The pipeline runs unchanged either way.
///
/// The key is the set of deterministically keyword-matched skill names - the action, not the wording.
/// The literature calls this structured intent canonicalization and prefers it over embedding
/// similarity for tool-calling agents, because two queries close in vector space can still need
/// different tools. Keying on the action means "employees in Zurich" and "employees in Bern" share an
/// entry, which is correct here since this caches skill SELECTION rather than answers, while
/// "check email" and "send email" never do.
///
/// Shadow mode exists because a wrong cache hit would be invisible in production: the model would
/// simply receive a toolset that does not fit the question and answer with it - no exception, no log
/// line, just a worse answer. The number that decides whether this is safe to switch on is therefore
/// not the hit rate but the DISAGREEMENT rate: how often the cached toolset differs from what the
/// pipeline actually produced. At zero disagreement, switching it on is not a gamble.
///
/// This carries a deliberate expiry. RecipeSkillMarginEvaluator ran in shadow mode from 2026-07-17
/// collecting calibration data nobody ever evaluated, and charged a full reranker pass per turn for
/// it. A shadow run without a date to look at the numbers is pure overhead - see EvaluationDueDate.
/// </summary>
/// <param name="cache">Process-wide store; entries are small (a signature and up to ~20 skill names).</param>
/// <param name="configuration">Holds the opt-out flag.</param>
/// <param name="logger">Writes the grep-able [cache-shadow] line the evaluation reads.</param>
public sealed class ToolsetCacheShadow
{
    /// <summary>
    /// Date the collected numbers are to be evaluated and this either switched on or removed.
    /// </summary>
    public const string EvaluationDueDate = "2026-08-10";

    private const string CacheKeyPrefix = "toolset-shadow:";
    private const int SignatureHashBytes = 4;

    // Bounded so a long-running process cannot grow this without limit. Entries are tiny; the cap is
    // about predictability on a 2.5 GB production container, not about memory pressure.
    private const int MaxEntries = 20000;

    private static readonly TimeSpan EntryLifetime = TimeSpan.FromHours(12);

    private readonly IMemoryCache _cache;
    private readonly IConfiguration _configuration;
    private readonly ILogger<ToolsetCacheShadow> _logger;

    // Bumped whenever the knowledge index is rebuilt. Part of every key, so entries written against a
    // previous skill catalogue can never be read back: a language pack install shifts which keywords
    // match, which silently changes what a signature means.
    private int _generation;

    private int _entryCount;

    public ToolsetCacheShadow(
        IMemoryCache cache,
        IConfiguration configuration,
        ILogger<ToolsetCacheShadow> logger)
    {
        _cache = cache;
        _configuration = configuration;
        _logger = logger;
    }

    /// <summary>
    /// Invalidates every entry by moving to a new generation. Called after an index rebuild.
    /// </summary>
    public void Invalidate()
    {
        Interlocked.Increment(ref _generation);
        Interlocked.Exchange(ref _entryCount, 0);
    }

    /// <summary>
    /// Compares what an action-keyed cache would have returned against what the pipeline produced,
    /// logs the outcome, and records the current result for the next turn. Never changes the turn.
    /// </summary>
    /// <param name="keywordMatchedSkills">Deterministically matched skill names forming the key</param>
    /// <param name="userPermissions">Permissions of the caller; part of the key, since the toolset is permission-scoped</param>
    /// <param name="retrievedSkillNames">Skills the retrieval pipeline actually produced this turn</param>
    /// <param name="readOnlySkillNames">
    /// Names of the caller's permitted skills that only read. Everything NOT in this set counts as
    /// writing - deliberately conservative, so a skill that was disabled between the write and the
    /// read of a cache entry is counted as the more dangerous kind rather than the harmless one.
    /// </param>
    public void ObserveAndRecord(
        IReadOnlyList<string> keywordMatchedSkills,
        IReadOnlyCollection<string> userPermissions,
        IReadOnlyList<string> retrievedSkillNames,
        IReadOnlySet<string> readOnlySkillNames)
    {
        if (!_configuration.GetValue(KnowledgeIndexConstants.ToolsetCacheShadowConfigKey, true))
        {
            return;
        }

        // No keyword match means no action to key on. A real cache would not apply here either, so
        // these turns are counted as "not applicable" rather than as misses.
        if (keywordMatchedSkills.Count == 0)
        {
            _logger.LogInformation("[cache-shadow] sig=none applicable=0");
            return;
        }

        var signature = string.Join(",", keywordMatchedSkills.OrderBy(n => n, StringComparer.Ordinal));
        var permissionKey = string.Join(",", userPermissions.OrderBy(p => p, StringComparer.Ordinal));
        var cacheKey = $"{CacheKeyPrefix}{_generation}:{ShortHash(signature)}:{ShortHash(permissionKey)}";

        var actual = retrievedSkillNames.OrderBy(n => n, StringComparer.Ordinal).ToList();

        // The disagreement rate alone cannot decide the safe variant of this cache: serving a stale
        // toolset of read-only skills costs a worse answer, serving one that contains a write costs a
        // wrong action. Counting the writes on both sides tells the evaluation how much of the traffic
        // a "read-only turns may use the cache" rule would still cover, and how often a disagreement
        // involved a writing skill at all.
        var writes = CountWrites(actual, readOnlySkillNames);

        if (_cache.TryGetValue<List<string>>(cacheKey, out var cached) && cached is not null)
        {
            var missing = cached.Except(actual, StringComparer.Ordinal).Count();
            var extra = actual.Except(cached, StringComparer.Ordinal).Count();

            _logger.LogInformation(
                "[cache-shadow] sig={Signature} applicable=1 hit=1 same={Same} cached={Cached} actual={Actual} missing={Missing} extra={Extra} writes={Writes} cachedWrites={CachedWrites}",
                ShortHash(signature),
                missing == 0 && extra == 0 ? 1 : 0,
                cached.Count,
                actual.Count,
                missing,
                extra,
                writes,
                CountWrites(cached, readOnlySkillNames));
        }
        else
        {
            _logger.LogInformation(
                "[cache-shadow] sig={Signature} applicable=1 hit=0 actual={Actual} writes={Writes}",
                ShortHash(signature),
                actual.Count,
                writes);
        }

        Record(cacheKey, actual);
    }

    private void Record(string cacheKey, List<string> actual)
    {
        if (Volatile.Read(ref _entryCount) >= MaxEntries)
        {
            return;
        }

        _cache.Set(cacheKey, actual, new MemoryCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = EntryLifetime,
            Size = 1
        });

        Interlocked.Increment(ref _entryCount);
    }

    private static int CountWrites(IEnumerable<string> skillNames, IReadOnlySet<string> readOnlySkillNames)
    {
        return skillNames.Count(n => !readOnlySkillNames.Contains(n));
    }

    private static string ShortHash(string value)
    {
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(value));
        return Convert.ToHexString(hash, 0, SignatureHashBytes).ToLowerInvariant();
    }
}
