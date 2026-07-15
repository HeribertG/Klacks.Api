// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Default <see cref="IQualificationImportRepository"/>.
/// </summary>
/// <param name="context">Database context providing the Qualification DbSet</param>

using Klacks.Api.Domain.Interfaces.Staffs;
using Klacks.Api.Domain.Models.Staffs;
using Klacks.Api.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Klacks.Api.Infrastructure.Repositories.Staffs;

public class QualificationImportRepository : IQualificationImportRepository
{
    private readonly DataBaseContext _context;

    public QualificationImportRepository(DataBaseContext context)
    {
        _context = context;
    }

    public async Task<List<Qualification>> GetBySourceKeysAsync(IReadOnlyCollection<string> sourceKeys)
    {
        if (sourceKeys.Count == 0)
        {
            return [];
        }

        return await _context.Qualification
            .Where(q => !q.IsDeleted && sourceKeys.Contains(q.ImportSourceKey))
            .ToListAsync();
    }

    public void Add(Qualification qualification)
    {
        _context.Qualification.Add(qualification);
    }

    public void Update(Qualification qualification)
    {
        _context.Qualification.Update(qualification);
    }
}
