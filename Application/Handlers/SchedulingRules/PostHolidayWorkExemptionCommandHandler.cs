// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Creates a holiday-work exemption. Without this the exemption axis would exist only in the database:
/// the detector reports holiday work for everyone, and an operation that legally staffs holidays would
/// have no way to say so. Import identity is never accepted from the caller.
/// </summary>
/// <param name="request">Carries the description and the optional scheduling-rule scope</param>

using Klacks.Api.Application.Commands.SchedulingRules;
using Klacks.Api.Application.DTOs.Scheduling;
using Klacks.Api.Application.Mappers;
using Klacks.Api.Domain.Interfaces;
using Klacks.Api.Domain.Interfaces.Scheduling;
using Klacks.Api.Domain.Models.Scheduling;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Handlers.SchedulingRules;

public class PostHolidayWorkExemptionCommandHandler
    : BaseHandler, IRequestHandler<PostHolidayWorkExemptionCommand, HolidayWorkExemptionResource>
{
    private readonly IHolidayWorkExemptionRuleRepository _repository;
    private readonly IUnitOfWork _unitOfWork;

    public PostHolidayWorkExemptionCommandHandler(
        IHolidayWorkExemptionRuleRepository repository,
        IUnitOfWork unitOfWork,
        ILogger<PostHolidayWorkExemptionCommandHandler> logger)
        : base(logger)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
    }

    public async Task<HolidayWorkExemptionResource> Handle(
        PostHolidayWorkExemptionCommand request,
        CancellationToken cancellationToken)
    {
        return await ExecuteAsync(async () =>
        {
            var rule = new HolidayWorkExemptionRule
            {
                Id = Guid.NewGuid(),
                Description = request.Resource.Description,
                SchedulingRuleId = request.Resource.SchedulingRuleId,
            };

            _repository.Add(rule);
            await _unitOfWork.CompleteAsync();

            return HolidayWorkExemptionMapper.ToResource(rule);
        },
        "creating a holiday work exemption",
        new { request.Resource.SchedulingRuleId });
    }
}
