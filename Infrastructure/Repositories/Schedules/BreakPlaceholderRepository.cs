// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Application.Interfaces;
using Klacks.Api.Domain.Models.Schedules;
using Klacks.Api.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Klacks.Api.Infrastructure.Repositories
{
    public class BreakPlaceholderRepository : BaseRepository<BreakPlaceholder>, IBreakPlaceholderRepository
    {
        private readonly DataBaseContext _context;

        public BreakPlaceholderRepository(DataBaseContext context, ILogger<BreakPlaceholder> logger)
          : base(context, logger)
        {
            _context = context;
        }

        public async Task<List<BreakPlaceholder>> GetByClientAndRangeAsync(Guid clientId, DateTime from, DateTime until, CancellationToken cancellationToken = default)
        {
            return await _context.BreakPlaceholder
                .AsNoTracking()
                .Where(bp => !bp.IsDeleted && bp.ClientId == clientId && bp.From <= until && bp.Until >= from)
                .OrderBy(bp => bp.From)
                .ThenBy(bp => bp.Until)
                .ToListAsync(cancellationToken);
        }
    }
}
