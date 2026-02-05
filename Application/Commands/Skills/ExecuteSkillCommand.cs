using Klacks.Api.Infrastructure.Mediator;
using Klacks.Api.Application.DTOs.Skills;

namespace Klacks.Api.Application.Commands.Skills;

public record ExecuteSkillCommand(
    SkillExecuteRequest Request,
    Guid UserId,
    Guid TenantId,
    string UserName,
    List<string> UserPermissions
) : IRequest<SkillExecuteResponse>;
