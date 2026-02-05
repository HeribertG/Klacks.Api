using Klacks.Api.Application.Commands.Skills;
using Klacks.Api.Application.Mappers;
using Klacks.Api.Domain.Interfaces;
using Klacks.Api.Infrastructure.Mediator;
using Klacks.Api.Application.DTOs.Skills;
using Microsoft.Extensions.Logging;

namespace Klacks.Api.Application.Handlers.Skills;

public class ExecuteSkillCommandHandler : BaseHandler, IRequestHandler<ExecuteSkillCommand, SkillExecuteResponse>
{
    private readonly ISkillExecutor _skillExecutor;
    private readonly SkillMapper _mapper;

    public ExecuteSkillCommandHandler(
        ISkillExecutor skillExecutor,
        SkillMapper mapper,
        ILogger<ExecuteSkillCommandHandler> logger)
        : base(logger)
    {
        _skillExecutor = skillExecutor;
        _mapper = mapper;
    }

    public async Task<SkillExecuteResponse> Handle(ExecuteSkillCommand request, CancellationToken cancellationToken)
    {
        return await ExecuteAsync(async () =>
        {
            _logger.LogInformation("Executing skill {SkillName} for user {UserId}",
                request.Request.SkillName, request.UserId);

            var invocation = _mapper.ToInvocation(request.Request);
            var context = _mapper.ToContext(
                request.UserId,
                request.TenantId,
                request.UserName,
                request.UserPermissions);

            var result = await _skillExecutor.ExecuteAsync(invocation, context, cancellationToken);
            var response = _mapper.ToResponse(result);

            _logger.LogInformation("Skill {SkillName} executed, Success: {Success}",
                request.Request.SkillName, result.Success);

            return response;
        }, "executing skill", new { SkillName = request.Request.SkillName, UserId = request.UserId });
    }
}
