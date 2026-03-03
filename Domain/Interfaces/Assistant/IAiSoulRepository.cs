// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Domain.Models.Assistant;

namespace Klacks.Api.Domain.Interfaces.Assistant;

public interface IAiSoulRepository
{
    Task<AiSoul?> GetActiveAsync(CancellationToken cancellationToken = default);

    Task<List<AiSoul>> GetAllAsync(CancellationToken cancellationToken = default);

    Task<AiSoul?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task AddAsync(AiSoul soul, CancellationToken cancellationToken = default);

    Task UpdateAsync(AiSoul soul, CancellationToken cancellationToken = default);

    Task DeactivateAllAsync(CancellationToken cancellationToken = default);
}
