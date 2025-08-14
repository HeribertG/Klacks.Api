using AutoMapper;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Application.Queries;
using Klacks.Api.Presentation.DTOs.Schedules;
using MediatR;

namespace Klacks.Api.Application.Handlers.Absences
{
    public class GetQueryHandler : IRequestHandler<GetQuery<AbsenceResource>, AbsenceResource>
    {
        private readonly IAbsenceRepository _absenceRepository;
        private readonly IMapper _mapper;

        public GetQueryHandler(IAbsenceRepository absenceRepository, IMapper mapper)
        {
            _absenceRepository = absenceRepository;
            _mapper = mapper;
        }

        public async Task<AbsenceResource> Handle(GetQuery<AbsenceResource> request, CancellationToken cancellationToken)
        {
            var absence = await _absenceRepository.Get(request.Id);
            return _mapper.Map<AbsenceResource>(absence)!;
        }
    }
}
