using Klacks.Api.Domain.Models.Assistant;

namespace Klacks.Api.Domain.Interfaces.Assistant;

public interface IAgentSessionRepository
{
    Task<AgentSession> GetOrCreateAsync(Guid agentId, string sessionId, string userId, CancellationToken cancellationToken = default);
    Task<List<AgentSessionMessage>> GetActiveMessagesAsync(Guid sessionId, CancellationToken cancellationToken = default);
    Task<AgentSessionMessage> SaveMessageAsync(AgentSessionMessage message, CancellationToken cancellationToken = default);
    Task UpdateSessionAsync(AgentSession session, CancellationToken cancellationToken = default);
    Task<int> GetTokenCountEstAsync(Guid sessionId, CancellationToken cancellationToken = default);
    Task CompactMessagesAsync(Guid sessionId, string summaryContent, CancellationToken cancellationToken = default);
    Task<List<AgentSession>> GetUserSessionsAsync(string userId, int limit = 20, CancellationToken cancellationToken = default);
    Task ArchiveStaleSessionsAsync(int daysInactive = 30, CancellationToken cancellationToken = default);
}
