// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Repository for Klacksy navigation feedback entries used by the training pipeline.
/// </summary>

namespace Klacks.Api.Infrastructure.Repositories.Klacksy;

using Klacks.Api.Domain.Models.Klacksy;
using Klacks.Api.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

public sealed class KlacksyNavigationFeedbackRepository : IKlacksyNavigationFeedbackRepository
{
    private readonly DataBaseContext _db;

    public KlacksyNavigationFeedbackRepository(DataBaseContext db) => _db = db;

    public async Task AddAsync(KlacksyNavigationFeedback entity, CancellationToken ct)
    {
        entity.Timestamp = DateTime.UtcNow;
        _db.KlacksyNavigationFeedback.Add(entity);
        await _db.SaveChangesAsync(ct);
    }

    public async Task<IReadOnlyList<KlacksyNavigationFeedback>> QueryUnresolvedAsync(string locale, int take, CancellationToken ct)
    {
        List<KlacksyNavigationFeedback> list = await _db.KlacksyNavigationFeedback.AsNoTracking()
            .Where(x => x.Locale == locale && x.MatchedTargetId == null && !x.IsDeleted)
            .OrderByDescending(x => x.Timestamp)
            .Take(take)
            .ToListAsync(ct);
        return list;
    }

    public async Task<IReadOnlyList<KlacksyNavigationFeedback>> QueryByUtterancePatternAsync(string pattern, int take, CancellationToken ct)
    {
        List<KlacksyNavigationFeedback> list = await _db.KlacksyNavigationFeedback.AsNoTracking()
            .Where(x => EF.Functions.ILike(x.Utterance, $"%{pattern}%") && !x.IsDeleted)
            .Take(take)
            .ToListAsync(ct);
        return list;
    }
}
