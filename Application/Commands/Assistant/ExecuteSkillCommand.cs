// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Infrastructure.Mediator;
using Klacks.Api.Application.DTOs.Assistant;

namespace Klacks.Api.Application.Commands.Assistant;

public record ExecuteSkillCommand(
    SkillExecuteRequest Request,
    Guid UserId,
    Guid TenantId,
    string UserName,
    List<string> UserPermissions
) : IRequest<SkillExecuteResponse>;
