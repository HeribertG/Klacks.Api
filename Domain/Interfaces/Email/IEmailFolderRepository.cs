// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Domain.Models.Email;

namespace Klacks.Api.Domain.Interfaces.Email;

public interface IEmailFolderRepository
{
    Task<List<EmailFolder>> GetAllAsync();

    Task<EmailFolder?> GetByIdAsync(Guid id);

    Task AddAsync(EmailFolder folder);

    Task DeleteAsync(Guid id);

    Task<bool> ExistsByImapNameAsync(string imapFolderName);

    Task<EmailFolder?> GetByImapNameAsync(string imapFolderName);
    Task DeleteNonSystemByImapNamesAsync(IEnumerable<string> imapFolderNames);

    Task UpdateSortOrderAsync(Guid id, int sortOrder);

    Task UpdateSpecialUseAsync(Guid id, string specialUse);

    Task<string?> GetImapNameBySpecialUseAsync(string specialUse);
}
