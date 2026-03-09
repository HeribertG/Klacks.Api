// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Application.Commands;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Application.Mappers;
using Klacks.Api.Domain.Interfaces;
using Klacks.Api.Infrastructure.Mediator;
using Klacks.Api.Application.DTOs.Schedules;

namespace Klacks.Api.Application.Handlers.ScheduleNotes;

public class DeleteCommandHandler : BaseHandler, IRequestHandler<DeleteCommand<ScheduleNoteResource>, ScheduleNoteResource?>
{
    private readonly IScheduleNoteRepository _scheduleNoteRepository;
    private readonly ScheduleMapper _scheduleMapper;
    private readonly IUnitOfWork _unitOfWork;

    public DeleteCommandHandler(
        IScheduleNoteRepository scheduleNoteRepository,
        ScheduleMapper scheduleMapper,
        IUnitOfWork unitOfWork,
        ILogger<DeleteCommandHandler> logger)
        : base(logger)
    {
        _scheduleNoteRepository = scheduleNoteRepository;
        _scheduleMapper = scheduleMapper;
        _unitOfWork = unitOfWork;
    }

    public async Task<ScheduleNoteResource?> Handle(DeleteCommand<ScheduleNoteResource> request, CancellationToken cancellationToken)
    {
        var existingScheduleNote = await _scheduleNoteRepository.Get(request.Id);
        if (existingScheduleNote == null)
        {
            return null;
        }

        var scheduleNoteResource = _scheduleMapper.ToScheduleNoteResource(existingScheduleNote);

        await _scheduleNoteRepository.Delete(request.Id);
        await _unitOfWork.CompleteAsync();

        return scheduleNoteResource;
    }
}
