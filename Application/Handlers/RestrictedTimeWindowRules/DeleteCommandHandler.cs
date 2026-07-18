// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Handler for soft-deleting a restricted time window rule.
/// </summary>
/// <param name="request">Contains the Id of the restricted time window rule to delete</param>

using Klacks.Api.Application.Commands;
using Klacks.Api.Application.DTOs.Scheduling;
using Klacks.Api.Application.Mappers;
using Klacks.Api.Domain.Interfaces;
using Klacks.Api.Domain.Interfaces.Scheduling;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Handlers.RestrictedTimeWindowRules;

public class DeleteCommandHandler : BaseHandler, IRequestHandler<DeleteCommand<RestrictedTimeWindowRuleResource>, RestrictedTimeWindowRuleResource?>
{
    private readonly IRestrictedTimeWindowRuleRepository _repository;
    private readonly ScheduleMapper _mapper;
    private readonly IUnitOfWork _unitOfWork;

    public DeleteCommandHandler(
        IRestrictedTimeWindowRuleRepository repository,
        ScheduleMapper mapper,
        IUnitOfWork unitOfWork,
        ILogger<DeleteCommandHandler> logger)
        : base(logger)
    {
        _repository = repository;
        _mapper = mapper;
        _unitOfWork = unitOfWork;
    }

    public async Task<RestrictedTimeWindowRuleResource?> Handle(DeleteCommand<RestrictedTimeWindowRuleResource> request, CancellationToken cancellationToken)
    {
        return await ExecuteAsync(async () =>
        {
            var deleted = await _repository.DeleteAsync(request.Id);
            if (deleted == null)
            {
                throw new KeyNotFoundException($"Restricted time window rule with ID {request.Id} not found.");
            }

            var resource = _mapper.ToRestrictedTimeWindowRuleResource(deleted);
            await _unitOfWork.CompleteAsync();

            return resource;
        },
        "deleting restricted time window rule",
        new { RuleId = request.Id });
    }
}
