// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Handler for soft-deleting a counter rule.
/// </summary>
/// <param name="request">Contains the Id of the counter rule to delete</param>

using Klacks.Api.Application.Commands;
using Klacks.Api.Application.DTOs.Scheduling;
using Klacks.Api.Application.Mappers;
using Klacks.Api.Domain.Interfaces;
using Klacks.Api.Domain.Interfaces.Scheduling;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Handlers.CounterRules;

public class DeleteCommandHandler : BaseHandler, IRequestHandler<DeleteCommand<CounterRuleResource>, CounterRuleResource?>
{
    private readonly ICounterRuleRepository _repository;
    private readonly ScheduleMapper _mapper;
    private readonly IUnitOfWork _unitOfWork;

    public DeleteCommandHandler(
        ICounterRuleRepository repository,
        ScheduleMapper mapper,
        IUnitOfWork unitOfWork,
        ILogger<DeleteCommandHandler> logger)
        : base(logger)
    {
        _repository = repository;
        _mapper = mapper;
        _unitOfWork = unitOfWork;
    }

    public async Task<CounterRuleResource?> Handle(DeleteCommand<CounterRuleResource> request, CancellationToken cancellationToken)
    {
        return await ExecuteAsync(async () =>
        {
            var deleted = await _repository.DeleteAsync(request.Id);
            if (deleted == null)
            {
                throw new KeyNotFoundException($"Counter rule with ID {request.Id} not found.");
            }

            var resource = _mapper.ToCounterRuleResource(deleted);
            await _unitOfWork.CompleteAsync();

            return resource;
        },
        "deleting counter rule",
        new { RuleId = request.Id });
    }
}
