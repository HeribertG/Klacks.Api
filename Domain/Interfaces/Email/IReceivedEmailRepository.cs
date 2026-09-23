// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Domain.Models.Email;

namespace Klacks.Api.Domain.Interfaces.Email;

public interface IReceivedEmailRepository
{
    Task AddAsync(ReceivedEmail email);

    Task<ReceivedEmail?> GetByIdAsync(Guid id);

    Task<List<ReceivedEmail>> GetListAsync(int skip, int take);

    Task<bool> ExistsByMessageIdAsync(string messageId);

    Task<long> GetHighestImapUidAsync(string folder);

    Task UpdateAsync(ReceivedEmail email);

    Task DeleteAsync(Guid id);

    Task<int> GetTotalCountAsync();

    Task<List<ReceivedEmail>> GetListByFolderAsync(string folder, int skip, int take);

    Task<int> GetUnreadCountByFolderAsync(string folder);

    Task<int> GetTotalCountByFolderAsync(string folder);

    Task<Dictionary<string, (int Total, int Unread)>> GetAllFolderCountsAsync();

    Task DeleteByFolderAsync(string folder);

    Task<List<ReceivedEmail>> GetFilteredListAsync(string? folder, bool? isRead, bool sortAsc, int skip, int take);

    Task<int> GetFilteredCountAsync(string? folder, bool? isRead);

    Task MoveToFolderAsync(Guid id, string folder);

    Task<int> BulkMoveFolderAsync(string oldFolder, string newFolder);

    /// <summary>
    /// Tracked (not no-tracking) query for emails whose processing pipeline (spam-classify, client
    /// assignment, intent analysis) never completed — ProcessedAt is null. The returned instances are
    /// tracked by THIS call's DbContext only: EmailPollingBackgroundService.ProcessBatchAsync processes
    /// each one in its own DI scope and reloads it there via GetByIdAsync instead of reusing these
    /// instances, so a per-mail failure cannot poison later commits in the same poll cycle. Do not mutate
    /// and save these returned entities directly from the scope that fetched them; do not run another
    /// tracked query for the same Ids from that same scope afterwards either — EF's identity resolution
    /// would hand back these now-stale instances instead of the current row.
    /// </summary>
    Task<List<ReceivedEmail>> GetUnprocessedAsync(int take);
}
