// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Application.Mappers;
using Klacks.Api.Application.Commands;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Domain.Models.Schedules;
using Klacks.Api.Application.DTOs.Schedules;
using Klacks.Api.Infrastructure.Mediator;
using Klacks.Api.Domain.Exceptions;
using Klacks.Api.Domain.Interfaces;

namespace Klacks.Api.Application.Handlers.BreakPlaceholders;

public class PostCommandHandler : BaseHandler, IRequestHandler<PostCommand<BreakPlaceholderResource>, BreakPlaceholderResource?>
{
    private readonly IBreakPlaceholderRepository _breakPlaceholderRepository;
    private readonly ScheduleMapper _scheduleMapper;
    private readonly IUnitOfWork _unitOfWork;

    public PostCommandHandler(
        IBreakPlaceholderRepository breakPlaceholderRepository,
        ScheduleMapper scheduleMapper,
        IUnitOfWork unitOfWork,
        ILogger<PostCommandHandler> logger)
        : base(logger)
    {
        _breakPlaceholderRepository = breakPlaceholderRepository;
        _scheduleMapper = scheduleMapper;
        _unitOfWork = unitOfWork;
        }

    public async Task<BreakPlaceholderResource?> Handle(PostCommand<BreakPlaceholderResource> request, CancellationToken cancellationToken)
    {
        return await ExecuteAsync(async () =>
        {
            var breakEntity = _scheduleMapper.ToBreakPlaceholderEntity(request.Resource);
            await _breakPlaceholderRepository.Add(breakEntity);
            await _unitOfWork.CompleteAsync();
            return _scheduleMapper.ToBreakPlaceholderResource(breakEntity);
        },
        "creating break",
        new { });
    }
}
