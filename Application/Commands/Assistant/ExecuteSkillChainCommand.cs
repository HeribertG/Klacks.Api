using Klacks.Api.Infrastructure.Mediator;
using Klacks.Api.Application.DTOs.Assistant;

namespace Klacks.Api.Application.Commands.Assistant;

public record ExecuteSkillChainCommand(
    SkillChainExecuteRequest Request,
    Guid UserId,
    Guid TenantId,
    string UserName,
    List<string> UserPermissions
) : IRequest<IReadOnlyList<SkillExecuteResponse>>;
