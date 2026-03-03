// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Domain.Interfaces.Assistant;
using Klacks.Api.Domain.Models.Assistant;
using Klacks.Api.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Klacks.Api.Infrastructure.Repositories.Assistant;

public class UiControlRepository : IUiControlRepository
{
    private readonly DataBaseContext _context;

    public UiControlRepository(DataBaseContext context)
    {
        _context = context;
    }

    public async Task<List<UiControl>> GetByPageKeyAsync(string pageKey, CancellationToken cancellationToken = default)
    {
        return await _context.UiControls
            .Where(c => c.PageKey == pageKey)
            .OrderBy(c => c.SortOrder)
            .ThenBy(c => c.ControlKey)
            .AsNoTracking()
            .ToListAsync(cancellationToken);
    }

    public async Task<List<string>> GetDistinctPageKeysAsync(CancellationToken cancellationToken = default)
    {
        return await _context.UiControls
            .Select(c => c.PageKey)
            .Distinct()
            .OrderBy(k => k)
            .ToListAsync(cancellationToken);
    }

    public async Task<List<UiControl>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await _context.UiControls
            .OrderBy(c => c.PageKey)
            .ThenBy(c => c.SortOrder)
            .AsNoTracking()
            .ToListAsync(cancellationToken);
    }

    public async Task AddRangeAsync(IEnumerable<UiControl> controls, CancellationToken cancellationToken = default)
    {
        await _context.UiControls.AddRangeAsync(controls, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAsync(UiControl control, CancellationToken cancellationToken = default)
    {
        _context.UiControls.Update(control);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task UpsertAsync(UiControl control, CancellationToken cancellationToken = default)
    {
        var existing = await _context.UiControls
            .FirstOrDefaultAsync(c => c.PageKey == control.PageKey && c.ControlKey == control.ControlKey, cancellationToken);

        if (existing != null)
        {
            existing.Selector = control.Selector;
            existing.SelectorType = control.SelectorType;
            existing.ControlType = control.ControlType;
            existing.Label = control.Label;
            existing.Route = control.Route;
            existing.SortOrder = control.SortOrder;
            existing.IsDynamic = control.IsDynamic;
            existing.SelectorPattern = control.SelectorPattern;
            _context.UiControls.Update(existing);
        }
        else
        {
            await _context.UiControls.AddAsync(control, cancellationToken);
        }

        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task<int> GetCountAsync(CancellationToken cancellationToken = default)
    {
        return await _context.UiControls.CountAsync(cancellationToken);
    }
}
