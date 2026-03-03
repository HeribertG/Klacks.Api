// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Domain.Interfaces.Email;
using Klacks.Api.Domain.Models.Email;
using Klacks.Api.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Klacks.Api.Infrastructure.Repositories.Email;

public class EmailFolderRepository : IEmailFolderRepository
{
    private readonly DataBaseContext _context;

    public EmailFolderRepository(DataBaseContext context)
    {
        _context = context;
    }

    public async Task<List<EmailFolder>> GetAllAsync()
    {
        return await _context.EmailFolders
            .OrderBy(f => f.SortOrder)
            .AsNoTracking()
            .ToListAsync();
    }

    public async Task<EmailFolder?> GetByIdAsync(Guid id)
    {
        return await _context.EmailFolders.FirstOrDefaultAsync(f => f.Id == id);
    }

    public async Task AddAsync(EmailFolder folder)
    {
        await _context.EmailFolders.AddAsync(folder);
    }

    public async Task DeleteAsync(Guid id)
    {
        var folder = await _context.EmailFolders.FirstOrDefaultAsync(f => f.Id == id);
        if (folder != null)
        {
            _context.EmailFolders.Remove(folder);
        }
    }

    public async Task<bool> ExistsByImapNameAsync(string imapFolderName)
    {
        return await _context.EmailFolders.AnyAsync(f => f.ImapFolderName == imapFolderName);
    }
}
