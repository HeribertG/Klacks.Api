// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Application.Interfaces;
using Klacks.Api.Application.Mappers;
using Klacks.Api.Application.Queries;
using Klacks.Api.Application.DTOs.Schedules;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Handlers.Absences
{
    public class GetQueryHandler : BaseHandler, IRequestHandler<GetQuery<AbsenceResource>, AbsenceResource>
    {
        private readonly IAbsenceRepository _absenceRepository;
        private readonly SettingsMapper _settingsMapper;

        public GetQueryHandler(IAbsenceRepository absenceRepository, SettingsMapper settingsMapper, ILogger<GetQueryHandler> logger)
            : base(logger)
        {
            _absenceRepository = absenceRepository;
            _settingsMapper = settingsMapper;
        }

        public async Task<AbsenceResource> Handle(GetQuery<AbsenceResource> request, CancellationToken cancellationToken)
        {
            return await ExecuteAsync(async () =>
            {
                var absence = await _absenceRepository.Get(request.Id);

                if (absence == null)
                {
                    throw new KeyNotFoundException($"Absence with ID {request.Id} not found");
                }

                return _settingsMapper.ToAbsenceResource(absence);
            }, nameof(Handle), new { request.Id });
        }
    }
}
