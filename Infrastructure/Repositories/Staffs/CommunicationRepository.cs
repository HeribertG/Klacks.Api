// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

using Klacks.Api.Domain.Common;
using Klacks.Api.Infrastructure.Persistence;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Domain.Models.Settings;
using Klacks.Api.Domain.Models.Staffs;
using Klacks.Api.Domain.Services.Schedules;
using Microsoft.EntityFrameworkCore;

namespace Klacks.Api.Infrastructure.Repositories.Staffs;

public class CommunicationRepository : BaseRepository<Communication>, ICommunicationRepository
{
    public CommunicationRepository(DataBaseContext context, ILogger<Communication> logger)
      : base(context, logger)
    {
    }

    public async Task<List<Communication>> GetClient(Guid id)
    {
        return await this.context.Communication.Where(c => c.ClientId == id).ToListAsync();
    }

    public async Task<List<Communication>> GetPhoneEntriesAsync(IReadOnlyCollection<Guid> clientIds, CancellationToken cancellationToken = default)
    {
        if (clientIds.Count == 0)
        {
            return [];
        }

        var ids = clientIds.ToList();
        var phoneTypes = PreferredPhoneSelector.PhoneTypesByPreference.ToList();

        return await this.context.Communication
            .AsNoTracking()
            .Where(c => ids.Contains(c.ClientId) && phoneTypes.Contains(c.Type))
            .ToListAsync(cancellationToken);
    }

    public async Task<List<CommunicationType>> TypeList()
    {
        return await this.context.CommunicationType.ToListAsync();
    }
}
