// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Domain.Models.Email;

namespace Klacks.Api.Domain.Interfaces.Email;

public interface IReceivedEmailRepository
{
    Task AddAsync(ReceivedEmail email);

    Task<ReceivedEmail?> GetByIdAsync(Guid id);

    Task<List<ReceivedEmail>> GetListAsync(int skip, int take);

    Task<int> GetUnreadCountAsync();

    Task<bool> ExistsByMessageIdAsync(string messageId);

    Task<long> GetHighestImapUidAsync(string folder);

    Task UpdateAsync(ReceivedEmail email);

    Task DeleteAsync(Guid id);

    Task<int> GetTotalCountAsync();

    Task<List<ReceivedEmail>> GetListByFolderAsync(string folder, int skip, int take);

    Task<int> GetUnreadCountByFolderAsync(string folder);

    Task<int> GetTotalCountByFolderAsync(string folder);

    Task DeleteByFolderAsync(string folder);

    Task<List<ReceivedEmail>> GetFilteredListAsync(string? folder, bool? isRead, bool sortAsc, int skip, int take);

    Task<int> GetFilteredCountAsync(string? folder, bool? isRead);
}
