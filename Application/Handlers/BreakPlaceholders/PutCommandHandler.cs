// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Updates a break placeholder. A placeholder whose stored owner or whose new owner is outside the caller's
/// group visibility is refused exactly like a placeholder that does not exist; nothing is written.
/// </summary>
/// <param name="clientVisibilityGuard">Decides whether the calling user may see or write for the owning client</param>

using Klacks.Api.Application.Mappers;
using Klacks.Api.Application.Commands;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Application.DTOs.Schedules;
using Klacks.Api.Infrastructure.Mediator;
using Klacks.Api.Domain.Exceptions;
using Klacks.Api.Domain.Interfaces;

namespace Klacks.Api.Application.Handlers.BreakPlaceholders;

public class PutCommandHandler : BaseHandler, IRequestHandler<PutCommand<BreakPlaceholderResource>, BreakPlaceholderResource?>
{
    private readonly IBreakPlaceholderRepository _breakPlaceholderRepository;
    private readonly IClientVisibilityGuard _clientVisibilityGuard;
    private readonly ScheduleMapper _scheduleMapper;
    private readonly IUnitOfWork _unitOfWork;

    public PutCommandHandler(
        IBreakPlaceholderRepository breakPlaceholderRepository,
        IClientVisibilityGuard clientVisibilityGuard,
        ScheduleMapper scheduleMapper,
        IUnitOfWork unitOfWork,
        ILogger<PutCommandHandler> logger)
        : base(logger)
    {
        _breakPlaceholderRepository = breakPlaceholderRepository;
        _clientVisibilityGuard = clientVisibilityGuard;
        _scheduleMapper = scheduleMapper;
        _unitOfWork = unitOfWork;
        }

    public async Task<BreakPlaceholderResource?> Handle(PutCommand<BreakPlaceholderResource> request, CancellationToken cancellationToken)
    {
        return await ExecuteAsync(async () =>
        {
            var existingBreak = await _breakPlaceholderRepository.Get(request.Resource.Id);
            if (existingBreak == null
                || !await _clientVisibilityGuard.AreAllVisibleAsync(
                    [existingBreak.ClientId, request.Resource.ClientId], cancellationToken))
            {
                throw new KeyNotFoundException($"Break with ID {request.Resource.Id} not found.");
            }

            _scheduleMapper.UpdateBreakEntity(request.Resource, existingBreak);
            await _unitOfWork.CompleteAsync();
            return _scheduleMapper.ToBreakPlaceholderResource(existingBreak);
        },
        "updating break",
        new { BreakPlaceholderId = request.Resource.Id });
    }
}
