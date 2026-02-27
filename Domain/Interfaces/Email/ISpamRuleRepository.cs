// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Domain.Models.Email;

namespace Klacks.Api.Domain.Interfaces.Email;

public interface ISpamRuleRepository
{
    Task<List<SpamRule>> GetAllActiveAsync();

    Task<List<SpamRule>> GetAllAsync();

    Task<SpamRule?> GetByIdAsync(Guid id);

    Task AddAsync(SpamRule spamRule);

    Task UpdateAsync(SpamRule spamRule);

    Task DeleteAsync(Guid id);
}
