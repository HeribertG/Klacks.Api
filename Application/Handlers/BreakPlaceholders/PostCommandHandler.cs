// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Creates a break placeholder. A placeholder for a client outside the caller's group visibility is refused
/// exactly like a placeholder for a client that does not exist; nothing is written.
/// </summary>
/// <param name="clientVisibilityGuard">Decides whether the calling user may see or write for the owning client</param>

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
    private readonly IClientVisibilityGuard _clientVisibilityGuard;
    private readonly ScheduleMapper _scheduleMapper;
    private readonly IUnitOfWork _unitOfWork;

    public PostCommandHandler(
        IBreakPlaceholderRepository breakPlaceholderRepository,
        IClientVisibilityGuard clientVisibilityGuard,
        ScheduleMapper scheduleMapper,
        IUnitOfWork unitOfWork,
        ILogger<PostCommandHandler> logger)
        : base(logger)
    {
        _breakPlaceholderRepository = breakPlaceholderRepository;
        _clientVisibilityGuard = clientVisibilityGuard;
        _scheduleMapper = scheduleMapper;
        _unitOfWork = unitOfWork;
        }

    public async Task<BreakPlaceholderResource?> Handle(PostCommand<BreakPlaceholderResource> request, CancellationToken cancellationToken)
    {
        return await ExecuteAsync(async () =>
        {
            var breakEntity = _scheduleMapper.ToBreakPlaceholderEntity(request.Resource);
            if (!await _clientVisibilityGuard.IsVisibleAsync(breakEntity.ClientId, cancellationToken))
            {
                throw new KeyNotFoundException($"Client with ID {breakEntity.ClientId} not found");
            }

            await _breakPlaceholderRepository.Add(breakEntity);
            await _unitOfWork.CompleteAsync();
            return _scheduleMapper.ToBreakPlaceholderResource(breakEntity);
        },
        "creating break",
        new { });
    }
}
