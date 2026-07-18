// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Handler for updating a period cap rule. Never touches ImportSourceKey/ImportContentHash: for
/// import-sourced rows this intentionally makes the live values diverge from the stored content hash,
/// so the next region-setup re-import detects the row as customer-edited and skips it (SkipEdited).
/// </summary>
/// <param name="request">Contains the period cap rule resource with Id and the updated editable fields</param>

using Klacks.Api.Application.Commands;
using Klacks.Api.Application.DTOs.Scheduling;
using Klacks.Api.Application.Mappers;
using Klacks.Api.Domain.Interfaces;
using Klacks.Api.Domain.Interfaces.Scheduling;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Handlers.PeriodCapRules;

public class PutCommandHandler : BaseHandler, IRequestHandler<PutCommand<PeriodCapRuleResource>, PeriodCapRuleResource?>
{
    private readonly IPeriodCapRuleRepository _repository;
    private readonly ScheduleMapper _mapper;
    private readonly IUnitOfWork _unitOfWork;

    public PutCommandHandler(
        IPeriodCapRuleRepository repository,
        ScheduleMapper mapper,
        IUnitOfWork unitOfWork,
        ILogger<PutCommandHandler> logger)
        : base(logger)
    {
        _repository = repository;
        _mapper = mapper;
        _unitOfWork = unitOfWork;
    }

    public async Task<PeriodCapRuleResource?> Handle(PutCommand<PeriodCapRuleResource> request, CancellationToken cancellationToken)
    {
        PeriodCapRuleValidation.Validate(request.Resource);

        return await ExecuteAsync(async () =>
        {
            var existing = await _repository.GetAsync(request.Resource!.Id);
            if (existing == null)
            {
                throw new KeyNotFoundException($"Period cap rule with ID {request.Resource.Id} not found.");
            }

            _mapper.UpdatePeriodCapRuleEntity(existing, request.Resource);
            _repository.Update(existing);
            await _unitOfWork.CompleteAsync();

            return _mapper.ToPeriodCapRuleResource(existing);
        },
        "updating period cap rule",
        new { RuleId = request.Resource?.Id, request.Resource?.Period });
    }
}
