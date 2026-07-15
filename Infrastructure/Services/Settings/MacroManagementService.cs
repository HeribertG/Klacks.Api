// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Domain.Enums;
using Klacks.Api.Domain.Exceptions;
using Klacks.Api.Domain.Interfaces;
using Klacks.Api.Domain.Models.Settings;
using Klacks.Api.Infrastructure.Interfaces;
using Klacks.Api.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Klacks.Api.Infrastructure.Services.Settings;

public class MacroManagementService : IMacroManagementService
{
    private readonly DataBaseContext _context;
    private readonly IMacroCache _macroCache;
    private readonly ILogger<MacroManagementService> _logger;

    public MacroManagementService(DataBaseContext context, IMacroCache macroCache, ILogger<MacroManagementService> logger)
    {
        _context = context;
        _macroCache = macroCache;
        _logger = logger;
    }

    public async Task<Macro> AddMacroAsync(Macro macro)
    {
        _logger.LogInformation("Adding new macro with ID: {MacroId}", macro.Id);
        await _context.Macro.AddAsync(macro);
        _macroCache.Invalidate(macro.Id);
        return macro;
    }

    public async Task<Macro> DeleteMacroAsync(Guid id)
    {
        _logger.LogInformation("Deleting macro with ID: {MacroId}", id);
        var macro = await _context.Macro.FindAsync(id);

        if (macro == null)
        {
            _logger.LogWarning("Macro with ID: {MacroId} not found for deletion", id);
            throw new InvalidOperationException($"Macro with ID {id} not found");
        }

        var referencingShiftCount = await _context.Shift.CountAsync(s => s.MacroId == id);
        if (referencingShiftCount > 0)
        {
            throw new InvalidRequestException(
                $"Macro '{macro.Name}' is still referenced by {referencingShiftCount} active shift(s). " +
                "Reassign the affected shifts to a different macro first, then delete this one.");
        }

        if ((MacroFunctionEnum)macro.Type != MacroFunctionEnum.Custom)
        {
            throw new InvalidRequestException(
                $"Macro '{macro.Name}' carries the {(MacroFunctionEnum)macro.Type} function for category " +
                $"'{macro.Category}'. Assign that function to another macro first, then delete this one.");
        }

        _context.Macro.Remove(macro);
        _macroCache.Invalidate(id);
        return macro;
    }

    public async Task<Macro> GetMacroAsync(Guid id)
    {
        _logger.LogInformation("Retrieving macro with ID: {MacroId}", id);
        var macro = await _context.Macro.FindAsync(id);

        if (macro == null)
        {
            _logger.LogWarning("Macro with ID: {MacroId} not found", id);
            throw new InvalidOperationException($"Macro with ID {id} not found");
        }

        return macro;
    }

    public async Task<List<Macro>> GetMacroListAsync()
    {
        _logger.LogInformation("Retrieving all macros");
        return await _context.Macro.Where(m => !m.IsDeleted).ToListAsync();
    }

    public async Task<bool> MacroExistsAsync(Guid id)
    {
        return await _context.Macro.AnyAsync(e => e.Id == id);
    }

    public Task<Macro> UpdateMacroAsync(Macro macro)
    {
        _logger.LogInformation("Updating macro with ID: {MacroId}", macro.Id);
        _context.Macro.Update(macro);
        _macroCache.Invalidate(macro.Id);
        return Task.FromResult(macro);
    }
}
