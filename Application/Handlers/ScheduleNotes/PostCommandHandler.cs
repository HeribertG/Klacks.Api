// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Creates a schedule note. A note for a client outside the caller's group visibility is refused exactly
/// like a note for a client that does not exist; nothing is written.
/// </summary>
/// <param name="clientVisibilityGuard">Decides whether the calling user may write for the client</param>

using Klacks.Api.Application.Commands;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Application.Mappers;
using Klacks.Api.Domain.Interfaces;
using Klacks.Api.Infrastructure.Mediator;
using Klacks.Api.Application.DTOs.Schedules;

namespace Klacks.Api.Application.Handlers.ScheduleNotes;

public class PostCommandHandler : BaseHandler, IRequestHandler<PostCommand<ScheduleNoteResource>, ScheduleNoteResource?>
{
    private readonly IScheduleNoteRepository _scheduleNoteRepository;
    private readonly IClientVisibilityGuard _clientVisibilityGuard;
    private readonly ScheduleMapper _scheduleMapper;
    private readonly IUnitOfWork _unitOfWork;

    public PostCommandHandler(
        IScheduleNoteRepository scheduleNoteRepository,
        IClientVisibilityGuard clientVisibilityGuard,
        ScheduleMapper scheduleMapper,
        IUnitOfWork unitOfWork,
        ILogger<PostCommandHandler> logger)
        : base(logger)
    {
        _scheduleNoteRepository = scheduleNoteRepository;
        _clientVisibilityGuard = clientVisibilityGuard;
        _scheduleMapper = scheduleMapper;
        _unitOfWork = unitOfWork;
    }

    public async Task<ScheduleNoteResource?> Handle(PostCommand<ScheduleNoteResource> request, CancellationToken cancellationToken)
    {
        return await ExecuteAsync(async () =>
        {
            var scheduleNote = _scheduleMapper.ToScheduleNoteEntity(request.Resource);
            if (!await _clientVisibilityGuard.IsVisibleAsync(scheduleNote.ClientId, cancellationToken))
            {
                throw new KeyNotFoundException($"Client with ID {scheduleNote.ClientId} not found");
            }

            await _scheduleNoteRepository.Add(scheduleNote);
            await _unitOfWork.CompleteAsync();

            return _scheduleMapper.ToScheduleNoteResource(scheduleNote);
        }, "CreateScheduleNote", new { request.Resource.Id });
    }
}
