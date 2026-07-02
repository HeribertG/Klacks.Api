// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Domain.Interfaces.Imports;
using Klacks.Api.Domain.Models.Imports;
using Klacks.Api.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Klacks.Api.Infrastructure.Repositories.Imports;

public class ErpImportExceptionRepository : BaseRepository<ErpImportException>, IErpImportExceptionRepository
{
    private readonly DataBaseContext _context;

    public ErpImportExceptionRepository(DataBaseContext context, ILogger<ErpImportException> logger)
        : base(context, logger)
    {
        _context = context;
    }

    public async Task<List<ErpImportException>> GetOpenAsync(CancellationToken cancellationToken = default)
    {
        return await _context.Set<ErpImportException>()
            .AsNoTracking()
            .Where(e => e.ResolvedAt == null)
            .OrderByDescending(e => e.CreateTime)
            .ToListAsync(cancellationToken);
    }
}
