// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// EF Core-backed repository for InboundAnalysis rows.
/// </summary>

using Klacks.Api.Domain.Enums;
using Klacks.Api.Domain.Interfaces.Inbound;
using Klacks.Api.Domain.Models.Inbound;
using Klacks.Api.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Klacks.Api.Infrastructure.Repositories.Inbound;

public class InboundAnalysisRepository : IInboundAnalysisRepository
{
    private readonly DataBaseContext _context;

    public InboundAnalysisRepository(DataBaseContext context)
    {
        _context = context;
    }

    public async Task AddAsync(InboundAnalysis analysis, CancellationToken cancellationToken = default)
    {
        await _context.InboundAnalyses.AddAsync(analysis, cancellationToken);
    }

    public async Task<InboundAnalysis?> GetBySourceAsync(
        InboundSourceKind sourceKind, Guid sourceId, CancellationToken cancellationToken = default)
    {
        return await _context.InboundAnalyses
            .FirstOrDefaultAsync(a => a.SourceKind == sourceKind && a.SourceId == sourceId, cancellationToken);
    }

    public async Task<bool> ExistsBySourceAsync(
        InboundSourceKind sourceKind, Guid sourceId, CancellationToken cancellationToken = default)
    {
        return await _context.InboundAnalyses
            .AsNoTracking()
            .IgnoreQueryFilters()
            .AnyAsync(a => a.SourceKind == sourceKind && a.SourceId == sourceId, cancellationToken);
    }
}
