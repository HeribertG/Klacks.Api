// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Repository interface for message persistence operations.
/// </summary>
using Klacks.Api.Domain.Enums;
using Klacks.Api.Domain.Models.Messaging;

namespace Klacks.Api.Domain.Interfaces.Messaging;

public interface IMessageRepository
{
    Task<Message?> GetByIdAsync(Guid id);

    Task<IReadOnlyList<Message>> GetMessagesAsync(Guid? providerId, MessageDirection? direction, string? sender, int count, int offset);

    Task<int> GetMessageCountAsync(Guid providerId);

    Task AddAsync(Message message);

    Task DeleteOldestMessagesAsync(Guid providerId, int retainCount);
}
