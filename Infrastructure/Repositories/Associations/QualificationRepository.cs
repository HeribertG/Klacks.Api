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
        return await context.Qualification
            .FirstOrDefaultAsync(q => q.Name.ToLower() == normalized, ct);
    }

    public async Task<List<Qualification>> GetAllAsync(CancellationToken ct = default)
    {
        return await context.Qualification
            .AsNoTracking()
            .OrderBy(q => q.Name)
            .ToListAsync(ct);
    }
}
