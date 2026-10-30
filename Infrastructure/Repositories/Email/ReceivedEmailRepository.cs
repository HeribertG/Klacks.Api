// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Repository for received emails: CRUD, folder moves and filtered list/count queries.
/// Filtered queries explicitly exclude soft-deleted rows (IsDeleted) in addition to the global EF query filter.
/// </summary>
/// <param name="folder">Optional email folder name to filter by</param>
/// <param name="isRead">Optional read-state filter</param>

using Klacks.Api.Domain.Interfaces.Email;
using Klacks.Api.Domain.Models.Email;
using Klacks.Api.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Klacks.Api.Infrastructure.Repositories.Email;

public class ReceivedEmailRepository : IReceivedEmailRepository
{
    private const string AtSign = "@";

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

    public async Task<bool> ExistsByMessageIdAsync(string messageId)
    {
        return await _context.ReceivedEmails.AnyAsync(e => e.MessageId == messageId);
    }

    public async Task<long> GetHighestImapUidAsync(string folder)
    {
        var hasAny = await _context.ReceivedEmails.AnyAsync(e => e.SourceImapFolder == folder);
        if (!hasAny) return 0;

        return await _context.ReceivedEmails
            .Where(e => e.SourceImapFolder == folder)
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

    public async Task<Dictionary<string, (int Total, int Unread)>> GetAllFolderCountsAsync()
    {
        return await _context.ReceivedEmails
            .GroupBy(e => e.Folder)
            .Select(g => new
            {
                Folder = g.Key,
                Total = g.Count(),
                Unread = g.Count(e => !e.IsRead)
            })
            .ToDictionaryAsync(
                x => x.Folder,
                x => (x.Total, x.Unread));
    }

    public async Task DeleteByFolderAsync(string folder)
    {
        var emails = await _context.ReceivedEmails.Where(e => e.Folder == folder).ToListAsync();
        _context.ReceivedEmails.RemoveRange(emails);
    }

    public async Task<List<ReceivedEmail>> GetFilteredListAsync(
        string? folder, bool? isRead, bool sortAsc, int skip, int take,
        IReadOnlyCollection<string>? excludedFromAddresses = null)
    {
        var query = BuildFilteredQuery(folder, isRead, excludedFromAddresses);

        query = sortAsc
            ? query.OrderBy(e => e.ReceivedDate)
            : query.OrderByDescending(e => e.ReceivedDate);

        return await query.Skip(skip).Take(take).AsNoTracking().ToListAsync();
    }

    public async Task<int> GetFilteredCountAsync(
        string? folder, bool? isRead, IReadOnlyCollection<string>? excludedFromAddresses = null)
    {
        return await BuildFilteredQuery(folder, isRead, excludedFromAddresses).CountAsync();
    }

    private IQueryable<ReceivedEmail> BuildFilteredQuery(
        string? folder, bool? isRead, IReadOnlyCollection<string>? excludedFromAddresses)
    {
        var query = _context.ReceivedEmails.Where(e => !e.IsDeleted);

        if (!string.IsNullOrWhiteSpace(folder))
            query = query.Where(e => e.Folder == folder);

        if (isRead.HasValue)
            query = query.Where(e => e.IsRead == isRead.Value);

        if (excludedFromAddresses is { Count: > 0 })
        {
            var excluded = excludedFromAddresses.ToList();
            query = query.Where(e => !excluded.Contains(e.FromAddress.ToLower()));
        }

        return query;
    }

    public async Task MoveToFolderAsync(Guid id, string folder)
    {
        var email = await _context.ReceivedEmails.FirstOrDefaultAsync(e => e.Id == id);
        if (email != null)
        {
            email.Folder = folder;
        }
    }

    public async Task<int> BulkMoveFolderAsync(string oldFolder, string newFolder)
    {
        return await _context.ReceivedEmails
            .Where(e => e.Folder == oldFolder)
            .ExecuteUpdateAsync(s => s.SetProperty(e => e.Folder, newFolder));
    }

    public async Task<List<ReceivedEmail>> GetUnprocessedAsync(int take)
    {
        return await _context.ReceivedEmails
            .Where(e => e.ProcessedAt == null)
            .OrderBy(e => e.ReceivedDate)
            .Take(take)
            .ToListAsync();
    }

    public async Task<SenderMailCounts> GetSenderMailCountsAsync(
        string folder, IReadOnlyCollection<string>? excludedFromAddresses, int maxSenders,
        CancellationToken cancellationToken = default)
    {
        var query = BuildFilteredQuery(folder, null, excludedFromAddresses);
        var totalMailCount = await query.CountAsync(cancellationToken);

        var rows = await query
            .GroupBy(e => e.FromAddress)
            .Select(g => new { Address = g.Key, Count = g.Count() })
            .OrderByDescending(row => row.Count)
            .ThenBy(row => row.Address)
            .Take(maxSenders + 1)
            .ToListAsync(cancellationToken);

        var senders = rows
            .Take(maxSenders)
            .Select(row => new SenderMailCount(row.Address, row.Count))
            .ToList();

        return new SenderMailCounts(senders, totalMailCount, rows.Count > maxSenders);
    }

    public async Task<List<SenderDomainMailCount>> GetMailCountsBySenderDomainAsync(
        IReadOnlyCollection<string> folders, IReadOnlyCollection<string> domains,
        CancellationToken cancellationToken = default)
    {
        if (folders.Count == 0 || domains.Count == 0)
        {
            return [];
        }

        var folderList = folders.ToList();
        var domainList = domains.Select(domain => domain.ToLowerInvariant()).Distinct().ToList();

        var rows = await _context.ReceivedEmails
            .Where(e => !e.IsDeleted && folderList.Contains(e.Folder) && e.FromAddress.Contains(AtSign))
            .Select(e => e.FromAddress.ToLower().Substring(e.FromAddress.IndexOf(AtSign) + 1))
            .Where(domain => domainList.Contains(domain))
            .GroupBy(domain => domain)
            .Select(g => new { Domain = g.Key, Count = g.Count() })
            .OrderBy(row => row.Domain)
            .ToListAsync(cancellationToken);

        return rows.Select(row => new SenderDomainMailCount(row.Domain, row.Count)).ToList();
    }
}
