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
}
