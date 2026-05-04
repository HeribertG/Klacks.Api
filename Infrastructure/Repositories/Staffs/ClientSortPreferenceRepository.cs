// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// EF Core implementation of IClientSortPreferenceRepository.
/// ReplaceAllAsync soft-deletes existing entries and stages the new set — the caller is responsible for calling IUnitOfWork.CompleteAsync().
/// </summary>

using Klacks.Api.Application.Interfaces;
using Klacks.Api.Domain.Models.Staffs;
using Klacks.Api.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Klacks.Api.Infrastructure.Repositories.Staffs;

public class ClientSortPreferenceRepository : IClientSortPreferenceRepository
{
    private readonly DataBaseContext _context;

    public ClientSortPreferenceRepository(DataBaseContext context)
    {
        _context = context;
    }

    public async Task<List<ClientSortPreference>> GetByUserAndGroupAsync(
        string userId, Guid groupId, CancellationToken cancellationToken = default)
    {
        return await _context.ClientSortPreference
            .Where(x => x.UserId == userId && x.GroupId == groupId)
            .OrderBy(x => x.SortOrder)
            .ToListAsync(cancellationToken);
    }

    public async Task ReplaceAllAsync(
        string userId, Guid groupId, IEnumerable<ClientSortPreference> newEntries)
    {
        var existing = await _context.ClientSortPreference
            .Where(x => x.UserId == userId && x.GroupId == groupId)
            .ToListAsync();

        _context.ClientSortPreference.RemoveRange(existing);
        await _context.ClientSortPreference.AddRangeAsync(newEntries);
    }
}
