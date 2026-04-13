// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Repository for managing custom speech-to-text provider configurations.
/// </summary>
using Klacks.Api.Domain.Models.Assistant;

namespace Klacks.Api.Domain.Interfaces.Assistant;

public interface ICustomSttProviderRepository
{
    Task<List<CustomSttProvider>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<List<CustomSttProvider>> GetEnabledAsync(CancellationToken cancellationToken = default);
    Task<CustomSttProvider?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task AddAsync(CustomSttProvider provider, CancellationToken cancellationToken = default);
    Task UpdateAsync(CustomSttProvider provider, CancellationToken cancellationToken = default);
    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}
