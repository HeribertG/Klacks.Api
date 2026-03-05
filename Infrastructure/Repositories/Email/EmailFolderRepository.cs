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

    public async Task<EmailFolder?> GetByImapNameAsync(string imapFolderName)
    {
        return await _context.EmailFolders.FirstOrDefaultAsync(f => f.ImapFolderName == imapFolderName);
    }

    public async Task DeleteNonSystemByImapNamesAsync(IEnumerable<string> imapFolderNames)
    {
        var nameSet = imapFolderNames.ToHashSet(StringComparer.OrdinalIgnoreCase);
        var toDelete = await _context.EmailFolders
            .Where(f => !f.IsSystem && !nameSet.Contains(f.ImapFolderName))
            .ToListAsync();
        _context.EmailFolders.RemoveRange(toDelete);
    }

    public async Task UpdateSortOrderAsync(Guid id, int sortOrder)
    {
        var folder = await _context.EmailFolders.FirstOrDefaultAsync(f => f.Id == id);
        if (folder != null)
        {
            folder.SortOrder = sortOrder;
        }
    }

    public async Task UpdateSpecialUseAsync(Guid id, string specialUse)
    {
        var folder = await _context.EmailFolders.FirstOrDefaultAsync(f => f.Id == id);
        if (folder != null)
        {
            folder.SpecialUse = specialUse;
        }
    }

    public async Task<string?> GetImapNameBySpecialUseAsync(string specialUse)
    {
        var folder = await _context.EmailFolders
            .AsNoTracking()
            .FirstOrDefaultAsync(f => f.SpecialUse == specialUse);
        return folder?.ImapFolderName;
    }
}
