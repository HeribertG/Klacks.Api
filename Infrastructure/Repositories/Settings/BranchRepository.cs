// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Domain.Common;
using Klacks.Api.Infrastructure.Persistence;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Domain.Models.Settings;
using Microsoft.EntityFrameworkCore;

namespace Klacks.Api.Infrastructure.Repositories.Settings;

public class BranchRepository : BaseRepository<Branch>, IBranchRepository
{
    private readonly DataBaseContext context;

    public BranchRepository(DataBaseContext context, ILogger<Branch> logger)
        : base(context, logger)
    {
        this.context = context;
    }

    public async Task<bool> ExistsByNameAsync(string name, Guid? excludeId = null)
    {
        var query = context.Set<Branch>()
            .Where(b => b.Name.ToLower() == name.ToLower());

        if (excludeId.HasValue)
            query = query.Where(b => b.Id != excludeId.Value);

        return await query.AnyAsync();
    }
}
