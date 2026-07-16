// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Handler for updating a counter rule. Never touches ImportSourceKey/ImportContentHash: for
/// import-sourced rows this intentionally makes the live values diverge from the stored content hash,
/// so the next region-setup re-import detects the row as customer-edited and skips it (SkipEdited).
/// </summary>
/// <param name="request">Contains the counter rule resource with Id and the updated editable fields</param>

using Klacks.Api.Application.Commands;
using Klacks.Api.Application.DTOs.Scheduling;
using Klacks.Api.Application.Mappers;
using Klacks.Api.Domain.Interfaces;
using Klacks.Api.Domain.Interfaces.Scheduling;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Handlers.CounterRules;

public class PutCommandHandler : BaseHandler, IRequestHandler<PutCommand<CounterRuleResource>, CounterRuleResource?>
{
    private readonly ICounterRuleRepository _repository;
    private readonly ScheduleMapper _mapper;
    private readonly IUnitOfWork _unitOfWork;

    public PutCommandHandler(
        ICounterRuleRepository repository,
        ScheduleMapper mapper,
        IUnitOfWork unitOfWork,
        ILogger<PutCommandHandler> logger)
        : base(logger)
    {
        _repository = repository;
        _mapper = mapper;
        _unitOfWork = unitOfWork;
    }

    public async Task<CounterRuleResource?> Handle(PutCommand<CounterRuleResource> request, CancellationToken cancellationToken)
    {
        CounterRuleValidation.Validate(request.Resource);

        return await ExecuteAsync(async () =>
        {
            var existing = await _repository.GetAsync(request.Resource!.Id);
            if (existing == null)
            {
                throw new KeyNotFoundException($"Counter rule with ID {request.Resource.Id} not found.");
            }

            _mapper.UpdateCounterRuleEntity(existing, request.Resource);
            _repository.Update(existing);
            await _unitOfWork.CompleteAsync();

            return _mapper.ToCounterRuleResource(existing);
        },
        "updating counter rule",
        new { RuleId = request.Resource?.Id, request.Resource?.EventType });
    }
}
