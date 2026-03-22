// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Repository interface for messaging provider persistence operations.
/// </summary>
using Klacks.Api.Domain.Models.Messaging;

namespace Klacks.Api.Domain.Interfaces.Messaging;

public interface IMessagingProviderRepository
{
    Task<MessagingProvider?> GetByIdAsync(Guid id);

    Task<MessagingProvider?> GetByNameAsync(string name);

    Task<IReadOnlyList<MessagingProvider>> GetAllAsync();

    Task<IReadOnlyList<MessagingProvider>> GetEnabledAsync();

    Task AddAsync(MessagingProvider provider);

    Task DeleteAsync(Guid id);
}
