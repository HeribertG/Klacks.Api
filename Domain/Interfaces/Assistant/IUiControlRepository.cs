// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Domain.Models.Assistant;

namespace Klacks.Api.Domain.Interfaces.Assistant;

public interface IUiControlRepository
{
    Task<List<UiControl>> GetByPageKeyAsync(string pageKey, CancellationToken cancellationToken = default);
    Task<List<string>> GetDistinctPageKeysAsync(CancellationToken cancellationToken = default);
    Task<List<UiControl>> GetAllAsync(CancellationToken cancellationToken = default);
    Task AddRangeAsync(IEnumerable<UiControl> controls, CancellationToken cancellationToken = default);
    Task UpdateAsync(UiControl control, CancellationToken cancellationToken = default);
    Task UpsertAsync(UiControl control, CancellationToken cancellationToken = default);
    Task<int> GetCountAsync(CancellationToken cancellationToken = default);
}
