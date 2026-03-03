// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Domain.Models.Assistant;

namespace Klacks.Api.Domain.Interfaces.Assistant;

public interface IAiGuidelinesRepository
{
    Task<AiGuidelines?> GetActiveAsync(CancellationToken cancellationToken = default);

    Task<List<AiGuidelines>> GetAllAsync(CancellationToken cancellationToken = default);

    Task<AiGuidelines?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task AddAsync(AiGuidelines guidelines, CancellationToken cancellationToken = default);

    Task UpdateAsync(AiGuidelines guidelines, CancellationToken cancellationToken = default);

    Task DeactivateAllAsync(CancellationToken cancellationToken = default);
}
