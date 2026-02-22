// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Application.Mappers;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Application.Queries;
using Klacks.Api.Application.DTOs.Schedules;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Handlers.Shifts;

public class GetQueryHandler : BaseHandler, IRequestHandler<GetQuery<ShiftResource>, ShiftResource>
{
    private readonly IShiftRepository _shiftRepository;
    private readonly ScheduleMapper _scheduleMapper;

    public GetQueryHandler(IShiftRepository shiftRepository, ScheduleMapper scheduleMapper, ILogger<GetQueryHandler> logger)
        : base(logger)
    {
        _shiftRepository = shiftRepository;
        _scheduleMapper = scheduleMapper;
    }

    public async Task<ShiftResource> Handle(GetQuery<ShiftResource> request, CancellationToken cancellationToken)
    {
        return await ExecuteAsync(async () =>
        {
            var shift = await _shiftRepository.Get(request.Id);

            if (shift == null)
            {
                throw new KeyNotFoundException($"Shift with ID {request.Id} not found");
            }

            return _scheduleMapper.ToShiftResource(shift);
        }, nameof(Handle), new { request.Id });
    }
}
