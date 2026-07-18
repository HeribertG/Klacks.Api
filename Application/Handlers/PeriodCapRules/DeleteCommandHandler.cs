// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Handler for soft-deleting a period cap rule.
/// </summary>
/// <param name="request">Contains the Id of the period cap rule to delete</param>

using Klacks.Api.Application.Commands;
using Klacks.Api.Application.DTOs.Scheduling;
using Klacks.Api.Application.Mappers;
using Klacks.Api.Domain.Interfaces;
using Klacks.Api.Domain.Interfaces.Scheduling;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Handlers.PeriodCapRules;

public class DeleteCommandHandler : BaseHandler, IRequestHandler<DeleteCommand<PeriodCapRuleResource>, PeriodCapRuleResource?>
{
    private readonly IPeriodCapRuleRepository _repository;
    private readonly ScheduleMapper _mapper;
    private readonly IUnitOfWork _unitOfWork;

    public DeleteCommandHandler(
        IPeriodCapRuleRepository repository,
        ScheduleMapper mapper,
        IUnitOfWork unitOfWork,
        ILogger<DeleteCommandHandler> logger)
        : base(logger)
    {
        _repository = repository;
        _mapper = mapper;
        _unitOfWork = unitOfWork;
    }

    public async Task<PeriodCapRuleResource?> Handle(DeleteCommand<PeriodCapRuleResource> request, CancellationToken cancellationToken)
    {
        return await ExecuteAsync(async () =>
        {
            var deleted = await _repository.DeleteAsync(request.Id);
            if (deleted == null)
            {
                throw new KeyNotFoundException($"Period cap rule with ID {request.Id} not found.");
            }

            var resource = _mapper.ToPeriodCapRuleResource(deleted);
            await _unitOfWork.CompleteAsync();

            return resource;
        },
        "deleting period cap rule",
        new { RuleId = request.Id });
    }
}
