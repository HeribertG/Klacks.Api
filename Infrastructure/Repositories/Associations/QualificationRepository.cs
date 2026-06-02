// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Repository for the Qualification master entity. GetByNameAsync supports create-or-return-existing
/// (case-insensitive) so the create skill does not produce duplicate qualifications.
/// </summary>

using Klacks.Api.Domain.Interfaces.Associations;
using Klacks.Api.Domain.Models.Staffs;
using Klacks.Api.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Klacks.Api.Infrastructure.Repositories.Associations;

public class QualificationRepository : BaseRepository<Qualification>, IQualificationRepository
{
    public QualificationRepository(DataBaseContext context, ILogger<Qualification> logger)
        : base(context, logger)
    {
    }

    public async Task<Qualification?> GetByNameAsync(string name, CancellationToken ct = default)
    {
        var normalized = name.Trim().ToLower();
        var all = await context.Qualification.ToListAsync(ct);
        return all.FirstOrDefault(q => q.Name?.De?.Trim().ToLower() == normalized);
    }

    public async Task<List<Qualification>> GetAllAsync(CancellationToken ct = default)
    {
        var list = await context.Qualification.AsNoTracking().ToListAsync(ct);
        return list.OrderBy(q => q.Name?.De ?? q.Name?.En ?? string.Empty).ToList();
    }
}
