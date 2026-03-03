// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Application.Commands.Assistant;
using Klacks.Api.Application.Mappers;
using Klacks.Api.Domain.Interfaces;
using Klacks.Api.Infrastructure.Mediator;
using Klacks.Api.Application.DTOs.Assistant;
using Microsoft.Extensions.Logging;

namespace Klacks.Api.Application.Handlers.Assistant;

public class ExecuteSkillChainCommandHandler : BaseHandler, IRequestHandler<ExecuteSkillChainCommand, IReadOnlyList<SkillExecuteResponse>>
{
    private readonly ISkillExecutor _skillExecutor;
    private readonly SkillMapper _mapper;

    public ExecuteSkillChainCommandHandler(
        ISkillExecutor skillExecutor,
        SkillMapper mapper,
        ILogger<ExecuteSkillChainCommandHandler> logger)
        : base(logger)
    {
        _skillExecutor = skillExecutor;
        _mapper = mapper;
    }

    public async Task<IReadOnlyList<SkillExecuteResponse>> Handle(ExecuteSkillChainCommand request, CancellationToken cancellationToken)
    {
        return await ExecuteAsync(async () =>
        {
            _logger.LogInformation("Executing skill chain with {Count} skills for user {UserId}",
                request.Request.Invocations.Count, request.UserId);

            var invocations = _mapper.ToInvocations(request.Request.Invocations);
            var context = _mapper.ToContext(
                request.UserId,
                request.TenantId,
                request.UserName,
                request.UserPermissions);

            var results = await _skillExecutor.ExecuteChainAsync(invocations, context, cancellationToken);
            var responses = _mapper.ToResponses(results);

            _logger.LogInformation("Skill chain executed, {SuccessCount}/{TotalCount} succeeded",
                results.Count(r => r.Success), results.Count);

            return responses;
        }, "executing skill chain", new { InvocationCount = request.Request.Invocations.Count, UserId = request.UserId });
    }
}
