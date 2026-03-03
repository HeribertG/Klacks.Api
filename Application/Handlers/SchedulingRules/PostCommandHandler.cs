// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Application.Mappers;
using Klacks.Api.Application.Commands;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Domain.Exceptions;
using Klacks.Api.Application.DTOs.Scheduling;
using Klacks.Api.Infrastructure.Mediator;
using Klacks.Api.Domain.Interfaces;

namespace Klacks.Api.Application.Handlers.SchedulingRules;

public class PostCommandHandler : BaseHandler, IRequestHandler<PostCommand<SchedulingRuleResource>, SchedulingRuleResource?>
{
    private readonly ISchedulingRuleRepository _repository;
    private readonly ScheduleMapper _mapper;
    private readonly IUnitOfWork _unitOfWork;

    public PostCommandHandler(
        ISchedulingRuleRepository repository,
        ScheduleMapper mapper,
        IUnitOfWork unitOfWork,
        ILogger<PostCommandHandler> logger)
        : base(logger)
    {
        _repository = repository;
        _mapper = mapper;
        _unitOfWork = unitOfWork;
    }

    public async Task<SchedulingRuleResource?> Handle(PostCommand<SchedulingRuleResource> request, CancellationToken cancellationToken)
    {
        Validate(request.Resource);

        return await ExecuteAsync(async () =>
        {
            var entity = _mapper.ToSchedulingRuleEntity(request.Resource);
            await _repository.Add(entity);
            await _unitOfWork.CompleteAsync();
            return _mapper.ToSchedulingRuleResource(entity);
        },
        "creating scheduling rule",
        new { request.Resource.Name });
    }

    private static void Validate(SchedulingRuleResource? resource)
    {
        if (resource == null)
        {
            throw new InvalidRequestException("Scheduling rule data is required.");
        }

        if (string.IsNullOrWhiteSpace(resource.Name))
        {
            throw new InvalidRequestException("Name is required.");
        }
    }
}
