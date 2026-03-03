// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Application.Interfaces;
using Klacks.Api.Domain.Interfaces;
using Klacks.Api.Domain.Models.Schedules;
using Klacks.Api.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Klacks.Api.Infrastructure.Repositories.Schedules;

public class WorkChangeRepository : BaseRepository<WorkChange>, IWorkChangeRepository
{
    private readonly DataBaseContext _context;
    private readonly IWorkMacroService _workMacroService;

    public WorkChangeRepository(
        DataBaseContext context,
        ILogger<WorkChange> logger,
        IWorkMacroService workMacroService)
        : base(context, logger)
    {
        _context = context;
        _workMacroService = workMacroService;
    }

    public override async Task Add(WorkChange entity)
    {
        await _workMacroService.ProcessWorkChangeMacroAsync(entity);
        await base.Add(entity);
    }

    public override async Task<WorkChange?> Put(WorkChange entity)
    {
        await _workMacroService.ProcessWorkChangeMacroAsync(entity);
        return await base.Put(entity);
    }

    public override async Task<WorkChange?> Get(Guid id)
    {
        return await _context.WorkChange
            .AsNoTracking()
            .Include(wc => wc.Work)
            .FirstOrDefaultAsync(wc => wc.Id == id);
    }
}
