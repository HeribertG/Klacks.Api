// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Default <see cref="IPlanningConstraintPresence"/>: one EXISTS query, cached process-wide for
/// PlanningConstraintDefaults.PresenceCacheSeconds. A change outside the invalidating handlers (manual DB edit) is
/// picked up after at most that time.
/// </summary>
/// <param name="repository">Planning constraint rows</param>
/// <param name="cache">Process-wide cache holding the answer</param>

using Klacks.Api.Application.Interfaces.Schedules;
using Klacks.Api.Domain.Constants;
using Klacks.Api.Domain.Interfaces.Scheduling;
using Microsoft.Extensions.Caching.Memory;

namespace Klacks.Api.Application.Services.Schedules.PlanningRules;

public sealed class PlanningConstraintPresence : IPlanningConstraintPresence
{
    private const string CacheKey = "planning-constraint-presence";

    private readonly IPlanningConstraintRepository _repository;
    private readonly IMemoryCache _cache;

    public PlanningConstraintPresence(IPlanningConstraintRepository repository, IMemoryCache cache)
    {
        _repository = repository;
        _cache = cache;
    }

    public async Task<bool> AnyApprovedAsync(CancellationToken cancellationToken = default)
    {
        if (_cache.TryGetValue(CacheKey, out bool cached))
        {
            return cached;
        }

        var any = await _repository.AnyApprovedAsync(cancellationToken);
        _cache.Set(CacheKey, any, new MemoryCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(PlanningConstraintDefaults.PresenceCacheSeconds),
            Size = 1,
        });
        return any;
    }

    public void Invalidate() => _cache.Remove(CacheKey);
}
