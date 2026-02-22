// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Application.Mappers;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Application.Queries;
using Klacks.Api.Application.DTOs.Schedules;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Handlers.AbsenceDetails;

public class GetQueryHandler : BaseHandler, IRequestHandler<GetQuery<AbsenceDetailResource>, AbsenceDetailResource>
{
    private readonly IAbsenceDetailRepository _absenceDetailRepository;
    private readonly SettingsMapper _settingsMapper;

    public GetQueryHandler(IAbsenceDetailRepository absenceDetailRepository, SettingsMapper settingsMapper, ILogger<GetQueryHandler> logger)
        : base(logger)
    {
        _absenceDetailRepository = absenceDetailRepository;
        _settingsMapper = settingsMapper;
    }

    public async Task<AbsenceDetailResource> Handle(GetQuery<AbsenceDetailResource> request, CancellationToken cancellationToken)
    {
        return await ExecuteAsync(async () =>
        {
            var absenceDetail = await _absenceDetailRepository.Get(request.Id);

            if (absenceDetail == null)
            {
                throw new KeyNotFoundException($"AbsenceDetail with ID {request.Id} not found");
            }

            return _settingsMapper.ToAbsenceDetailResource(absenceDetail);
        }, nameof(Handle), new { request.Id });
    }
}
