// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Deletes a break placeholder. A placeholder owned by a client outside the caller's group visibility is
/// refused exactly like a placeholder that does not exist; nothing is deleted.
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

public class DeleteCommandHandler : BaseHandler, IRequestHandler<DeleteCommand<BreakPlaceholderResource>, BreakPlaceholderResource?>
{
    private readonly IBreakPlaceholderRepository _breakPlaceholderRepository;
    private readonly IClientVisibilityGuard _clientVisibilityGuard;
    private readonly ScheduleMapper _scheduleMapper;
    private readonly IUnitOfWork _unitOfWork;

    public DeleteCommandHandler(
        IBreakPlaceholderRepository breakPlaceholderRepository,
        IClientVisibilityGuard clientVisibilityGuard,
        ScheduleMapper scheduleMapper,
        IUnitOfWork unitOfWork,
        ILogger<DeleteCommandHandler> logger)
        : base(logger)
    {
        _breakPlaceholderRepository = breakPlaceholderRepository;
        _clientVisibilityGuard = clientVisibilityGuard;
        _scheduleMapper = scheduleMapper;
        _unitOfWork = unitOfWork;
        }

    public async Task<BreakPlaceholderResource?> Handle(DeleteCommand<BreakPlaceholderResource> request, CancellationToken cancellationToken)
    {
        return await ExecuteAsync(async () =>
        {
            var existingBreak = await _breakPlaceholderRepository.Get(request.Id);
            if (existingBreak == null
                || !await _clientVisibilityGuard.IsVisibleAsync(existingBreak.ClientId, cancellationToken))
            {
                throw new KeyNotFoundException($"Break with ID {request.Id} not found.");
            }

            var breakResource = _scheduleMapper.ToBreakPlaceholderResource(existingBreak);
            await _breakPlaceholderRepository.Delete(request.Id);
            await _unitOfWork.CompleteAsync();

            return breakResource;
        },
        "deleting break",
        new { BreakPlaceholderId = request.Id });
    }
}
