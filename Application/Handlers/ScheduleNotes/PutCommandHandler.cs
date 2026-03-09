// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Application.Commands;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Application.Mappers;
using Klacks.Api.Domain.Interfaces;
using Klacks.Api.Infrastructure.Mediator;
using Klacks.Api.Application.DTOs.Schedules;

namespace Klacks.Api.Application.Handlers.ScheduleNotes;

public class PutCommandHandler : BaseHandler, IRequestHandler<PutCommand<ScheduleNoteResource>, ScheduleNoteResource?>
{
    private readonly IScheduleNoteRepository _scheduleNoteRepository;
    private readonly ScheduleMapper _scheduleMapper;
    private readonly IUnitOfWork _unitOfWork;

    public PutCommandHandler(
        IScheduleNoteRepository scheduleNoteRepository,
        ScheduleMapper scheduleMapper,
        IUnitOfWork unitOfWork,
        ILogger<PutCommandHandler> logger)
        : base(logger)
    {
        _scheduleNoteRepository = scheduleNoteRepository;
        _scheduleMapper = scheduleMapper;
        _unitOfWork = unitOfWork;
    }

    public async Task<ScheduleNoteResource?> Handle(PutCommand<ScheduleNoteResource> request, CancellationToken cancellationToken)
    {
        return await ExecuteAsync(async () =>
        {
            var existingScheduleNote = await _scheduleNoteRepository.GetNoTracking(request.Resource.Id);
            if (existingScheduleNote == null)
            {
                return null;
            }

            var scheduleNote = _scheduleMapper.ToScheduleNoteEntity(request.Resource);
            var updatedScheduleNote = await _scheduleNoteRepository.Put(scheduleNote);
            await _unitOfWork.CompleteAsync();

            return updatedScheduleNote != null ? _scheduleMapper.ToScheduleNoteResource(updatedScheduleNote) : null;
        }, "UpdateScheduleNote", new { request.Resource.Id });
    }
}
