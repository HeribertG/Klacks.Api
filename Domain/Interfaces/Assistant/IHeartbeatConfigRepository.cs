using Klacks.Api.Domain.Models.Assistant;

namespace Klacks.Api.Domain.Interfaces.Assistant;

public interface IHeartbeatConfigRepository
{
    Task<HeartbeatConfig?> GetByUserIdAsync(string userId, CancellationToken cancellationToken = default);

    Task<List<HeartbeatConfig>> GetAllEnabledAsync(CancellationToken cancellationToken = default);

    Task AddAsync(HeartbeatConfig config, CancellationToken cancellationToken = default);

    Task UpdateAsync(HeartbeatConfig config, CancellationToken cancellationToken = default);
}
