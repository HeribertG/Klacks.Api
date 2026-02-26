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
}
