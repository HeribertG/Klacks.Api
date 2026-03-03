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
}
