// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Domain.Interfaces.Email;
using Klacks.Api.Domain.Models.Email;
using Klacks.Api.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Klacks.Api.Infrastructure.Repositories.Email;

public class ReceivedEmailRepository : IReceivedEmailRepository
{
    private readonly DataBaseContext _context;
    private readonly ILogger<ReceivedEmailRepository> _logger;

    public ReceivedEmailRepository(DataBaseContext context, ILogger<ReceivedEmailRepository> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task AddAsync(ReceivedEmail email)
    {
        await _context.ReceivedEmails.AddAsync(email);
    }

    public async Task<ReceivedEmail?> GetByIdAsync(Guid id)
    {
        return await _context.ReceivedEmails.FirstOrDefaultAsync(e => e.Id == id);
    }

    public async Task<List<ReceivedEmail>> GetListAsync(int skip, int take)
    {
        return await _context.ReceivedEmails
            .OrderByDescending(e => e.ReceivedDate)
            .Skip(skip)
            .Take(take)
            .AsNoTracking()
            .ToListAsync();
    }

    public async Task<int> GetUnreadCountAsync()
    {
        return await _context.ReceivedEmails.CountAsync(e => !e.IsRead);
    }

    public async Task<bool> ExistsByMessageIdAsync(string messageId)
    {
        return await _context.ReceivedEmails.AnyAsync(e => e.MessageId == messageId);
    }

    public async Task<long> GetHighestImapUidAsync(string folder)
    {
        var hasAny = await _context.ReceivedEmails.AnyAsync(e => e.Folder == folder);
        if (!hasAny) return 0;

        return await _context.ReceivedEmails
            .Where(e => e.Folder == folder)
            .MaxAsync(e => e.ImapUid);
    }

    public Task UpdateAsync(ReceivedEmail email)
    {
        _context.ReceivedEmails.Update(email);
        return Task.CompletedTask;
    }

    public async Task DeleteAsync(Guid id)
    {
        var email = await _context.ReceivedEmails.FirstOrDefaultAsync(e => e.Id == id);
        if (email != null)
        {
            _context.ReceivedEmails.Remove(email);
        }
    }

    public async Task<int> GetTotalCountAsync()
    {
        return await _context.ReceivedEmails.CountAsync();
    }

    public async Task<List<ReceivedEmail>> GetListByFolderAsync(string folder, int skip, int take)
    {
        return await _context.ReceivedEmails
            .Where(e => e.Folder == folder)
            .OrderByDescending(e => e.ReceivedDate)
            .Skip(skip)
            .Take(take)
            .AsNoTracking()
            .ToListAsync();
    }

    public async Task<int> GetUnreadCountByFolderAsync(string folder)
    {
        return await _context.ReceivedEmails.CountAsync(e => e.Folder == folder && !e.IsRead);
    }

    public async Task<int> GetTotalCountByFolderAsync(string folder)
    {
        return await _context.ReceivedEmails.CountAsync(e => e.Folder == folder);
    }

    public async Task DeleteByFolderAsync(string folder)
    {
        var emails = await _context.ReceivedEmails.Where(e => e.Folder == folder).ToListAsync();
        _context.ReceivedEmails.RemoveRange(emails);
    }

    public async Task<List<ReceivedEmail>> GetFilteredListAsync(
        string? folder, bool? isRead, bool sortAsc, int skip, int take)
    {
        var query = _context.ReceivedEmails.AsQueryable();

        if (!string.IsNullOrWhiteSpace(folder))
            query = query.Where(e => e.Folder == folder);

        if (isRead.HasValue)
            query = query.Where(e => e.IsRead == isRead.Value);

        query = sortAsc
            ? query.OrderBy(e => e.ReceivedDate)
            : query.OrderByDescending(e => e.ReceivedDate);

        return await query.Skip(skip).Take(take).AsNoTracking().ToListAsync();
    }

    public async Task<int> GetFilteredCountAsync(string? folder, bool? isRead)
    {
        var query = _context.ReceivedEmails.AsQueryable();

        if (!string.IsNullOrWhiteSpace(folder))
            query = query.Where(e => e.Folder == folder);

        if (isRead.HasValue)
            query = query.Where(e => e.IsRead == isRead.Value);

        return await query.CountAsync();
    }
}
