// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Handler for creating a customer-facing period cap rule. New rows always get an empty
/// ImportSourceKey/ImportContentHash so they are never touched by the region-setup re-import.
/// </summary>
/// <param name="request">Contains the period cap rule resource with Period, Scope, CapHours, the
/// rolling-average fields and the optional SchedulingRuleId scope</param>

using Klacks.Api.Application.Commands;
using Klacks.Api.Application.DTOs.Scheduling;
using Klacks.Api.Application.Mappers;
using Klacks.Api.Domain.Interfaces;
using Klacks.Api.Domain.Interfaces.Scheduling;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Handlers.PeriodCapRules;

public class PostCommandHandler : BaseHandler, IRequestHandler<PostCommand<PeriodCapRuleResource>, PeriodCapRuleResource?>
{
    private readonly IPeriodCapRuleRepository _repository;
    private readonly ScheduleMapper _mapper;
    private readonly IUnitOfWork _unitOfWork;

    public PostCommandHandler(
        IPeriodCapRuleRepository repository,
        ScheduleMapper mapper,
        IUnitOfWork unitOfWork,
        ILogger<PostCommandHandler> logger)
        : base(logger)
    {
        _repository = repository;
        _mapper = mapper;
        _unitOfWork = unitOfWork;
    }

    public async Task<PeriodCapRuleResource?> Handle(PostCommand<PeriodCapRuleResource> request, CancellationToken cancellationToken)
    {
        PeriodCapRuleValidation.Validate(request.Resource);

        return await ExecuteAsync(async () =>
        {
            var entity = _mapper.ToPeriodCapRuleEntity(request.Resource!);
            entity.Id = Guid.NewGuid();
            entity.ImportSourceKey = string.Empty;
            entity.ImportContentHash = string.Empty;

            _repository.Add(entity);
            await _unitOfWork.CompleteAsync();
            return _mapper.ToPeriodCapRuleResource(entity);
        },
        "creating period cap rule",
        new { request.Resource?.Period, request.Resource?.Scope, request.Resource?.CapHours });
    }
}
