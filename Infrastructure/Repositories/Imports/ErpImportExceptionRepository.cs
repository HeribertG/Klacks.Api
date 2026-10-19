// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

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

    public async Task<List<ErpImportException>> GetByFileKeysAsync(IReadOnlyCollection<string> fileKeys, CancellationToken cancellationToken = default)
    {
        return await _context.Set<ErpImportException>()
            .AsNoTracking()
            .Where(e => fileKeys.Contains(e.FileKey))
            .OrderByDescending(e => e.CreateTime)
            .ToListAsync(cancellationToken);
    }

    public async Task<List<ErpImportException>> GetOpenRejectedOrdersAsync(IReadOnlyCollection<Guid> importFileIds, CancellationToken cancellationToken = default)
    {
        if (importFileIds.Count == 0)
        {
            return [];
        }

        return await _context.Set<ErpImportException>()
            .AsNoTracking()
            .Where(e => e.ImportFileId != null
                && importFileIds.Contains(e.ImportFileId.Value)
                && e.ResolvedAt == null
                && e.ExternalOrderReference != null)
            .OrderBy(e => e.CreateTime)
            .ThenBy(e => e.ExternalOrderReference)
            .ToListAsync(cancellationToken);
    }

    public async Task<int> ResolveOpenForImportFileAsync(Guid importFileId, DateTime resolvedAt, CancellationToken cancellationToken = default)
    {
        return await _context.Set<ErpImportException>()
            .Where(e => e.ImportFileId == importFileId && e.ResolvedAt == null)
            .ExecuteUpdateAsync(setters => setters.SetProperty(e => e.ResolvedAt, resolvedAt), cancellationToken);
    }

    public async Task<int> LinkToImportFileAsync(string fileKey, Guid importFileId, CancellationToken cancellationToken = default)
    {
        return await _context.Set<ErpImportException>()
            .Where(e => e.FileKey == fileKey && e.ImportFileId == null)
            .ExecuteUpdateAsync(setters => setters.SetProperty(e => e.ImportFileId, importFileId), cancellationToken);
    }

    public async Task<List<Guid>> GetIdsByImportFileAsync(Guid importFileId, CancellationToken cancellationToken = default)
    {
        return await _context.Set<ErpImportException>()
            .IgnoreQueryFilters()
            .AsNoTracking()
            .Where(e => e.ImportFileId == importFileId)
            .Select(e => e.Id)
            .ToListAsync(cancellationToken);
    }

    public async Task<int> DeleteByIdsAsync(IReadOnlyCollection<Guid> ids, CancellationToken cancellationToken = default)
    {
        if (ids.Count == 0)
        {
            return 0;
        }

        return await _context.Set<ErpImportException>()
            .IgnoreQueryFilters()
            .Where(e => ids.Contains(e.Id))
            .ExecuteDeleteAsync(cancellationToken);
    }
}
