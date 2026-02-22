// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Domain.Common;
using Klacks.Api.Infrastructure.Persistence;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Domain.Models.Settings;
using Klacks.Api.Domain.Models.Staffs;
using Microsoft.EntityFrameworkCore;

namespace Klacks.Api.Infrastructure.Repositories.Staffs;

public class CommunicationRepository : BaseRepository<Communication>, ICommunicationRepository
{
    private readonly DataBaseContext context;

    public CommunicationRepository(DataBaseContext context, ILogger<Communication> logger)
      : base(context, logger)
    {
        this.context = context;
    }

    public async Task<List<Communication>> GetClient(Guid id)
    {
        return await this.context.Communication.Where(c => c.ClientId == id).ToListAsync();
    }

    public async Task<List<CommunicationType>> TypeList()
    {
        return await this.context.CommunicationType.ToListAsync();
    }
}
