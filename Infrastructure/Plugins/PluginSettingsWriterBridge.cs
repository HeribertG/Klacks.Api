// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Bridges the Contracts IPluginSettingsWriter to the Core ISettingsRepository.
/// Plugins use this to persist plugin-owned settings keys.
/// </summary>

using Klacks.Api.Application.Interfaces;
using Klacks.Api.Infrastructure.Persistence;
using Klacks.Plugin.Contracts;
using Microsoft.EntityFrameworkCore;

namespace Klacks.Api.Infrastructure.Plugins;

public class PluginSettingsWriterBridge : IPluginSettingsWriter
{
    private readonly ISettingsRepository _repository;
    private readonly DataBaseContext _context;

    public PluginSettingsWriterBridge(ISettingsRepository repository, DataBaseContext context)
    {
        _repository = repository;
        _context = context;
    }

    public async Task SetSettingAsync(string key, string value, CancellationToken ct = default)
    {
        var existing = await _repository.GetSetting(key);
        if (existing == null)
        {
            await _repository.AddSetting(new Klacks.Api.Domain.Models.Settings.Settings
            {
                Id = Guid.NewGuid(),
                Type = key,
                Value = value
            });
        }
        else
        {
            existing.Value = value;
            await _repository.PutSetting(existing);
        }

        await _context.SaveChangesAsync(ct);
    }

    public async Task DeleteSettingAsync(string key, CancellationToken ct = default)
    {
        var existing = await _context.Settings.FirstOrDefaultAsync(s => s.Type == key, ct);
        if (existing != null)
        {
            _context.Settings.Remove(existing);
            await _context.SaveChangesAsync(ct);
        }
    }
}
