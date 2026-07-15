// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Default <see cref="IMacroImportRepository"/>.
/// </summary>
/// <param name="context">Database context providing the Macro DbSet</param>

using Klacks.Api.Domain.Enums;
using Klacks.Api.Domain.Interfaces.Settings;
using Klacks.Api.Domain.Models.Settings;
using Klacks.Api.Infrastructure.Interfaces;
using Klacks.Api.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Klacks.Api.Infrastructure.Repositories.Settings;

public class MacroImportRepository : IMacroImportRepository
{
    private readonly DataBaseContext _context;
    private readonly IMacroCache _macroCache;

    public MacroImportRepository(DataBaseContext context, IMacroCache macroCache)
    {
        _context = context;
        _macroCache = macroCache;
    }

    public async Task<List<Macro>> GetBySourceKeysAsync(IReadOnlyCollection<string> sourceKeys)
    {
        if (sourceKeys.Count == 0)
        {
            return [];
        }

        return await _context.Macro
            .Where(m => !m.IsDeleted && sourceKeys.Contains(m.ImportSourceKey))
            .ToListAsync();
    }

    public async Task<Macro?> GetActiveFunctionHolderAsync(MacroCategoryEnum category, MacroFunctionEnum function)
    {
        return await _context.Macro
            .FirstOrDefaultAsync(m => !m.IsDeleted && m.Category == category && m.Type == (int)function);
    }

    public void Add(Macro macro)
    {
        _context.Macro.Add(macro);
        _macroCache.Invalidate(macro.Id);
    }

    public void Update(Macro macro)
    {
        _context.Macro.Update(macro);
        _macroCache.Invalidate(macro.Id);
    }
}
