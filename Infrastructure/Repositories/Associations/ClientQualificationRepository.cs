// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Repository for ClientQualification. GetActiveAsync returns the tracked active row for a
/// (client, qualification) pair so the set-command handler can upsert it in place.
/// </summary>

using Klacks.Api.Domain.Interfaces.Associations;
using Klacks.Api.Domain.Models.Associations;
using Klacks.Api.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Klacks.Api.Infrastructure.Repositories.Associations;

public class ClientQualificationRepository : BaseRepository<ClientQualification>, IClientQualificationRepository
{
    public ClientQualificationRepository(DataBaseContext context, ILogger<ClientQualification> logger)
        : base(context, logger)
    {
    }

    public async Task<ClientQualification?> GetActiveAsync(
        Guid clientId, Guid qualificationId, CancellationToken ct = default)
    {
        return await context.ClientQualification
            .FirstOrDefaultAsync(cq => cq.ClientId == clientId && cq.QualificationId == qualificationId, ct);
    }

    public async Task<List<ClientQualification>> GetByClientIdsAsync(
        IReadOnlyCollection<Guid> clientIds, CancellationToken ct = default)
    {
        var ids = clientIds.ToList();
        return await context.ClientQualification
            .Where(cq => ids.Contains(cq.ClientId))
            .ToListAsync(ct);
    }
}
