using Klacks.Api.Application.Queries;
using Klacks.Api.Application.Services;
using Klacks.Api.Presentation.DTOs.Schedules;
using MediatR;

namespace Klacks.Api.Application.Handlers.Absences
{
    public class GetQueryHandler : IRequestHandler<GetQuery<AbsenceResource>, AbsenceResource>
    {
        private readonly AbsenceApplicationService _absenceApplicationService;

        public GetQueryHandler(AbsenceApplicationService absenceApplicationService)
        {
            _absenceApplicationService = absenceApplicationService;
        }

        public async Task<AbsenceResource> Handle(GetQuery<AbsenceResource> request, CancellationToken cancellationToken)
        {
            var absence = await _absenceApplicationService.GetAbsenceByIdAsync(request.Id, cancellationToken);
            return absence!;
        }
    }
}
