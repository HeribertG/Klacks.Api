using AutoMapper;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Application.Queries;
using Klacks.Api.Presentation.DTOs.Schedules;
using MediatR;

namespace Klacks.Api.Application.Handlers.Absences
{
    public class GetQueryHandler : IRequestHandler<GetQuery<AbsenceResource>, AbsenceResource>
    {
        private readonly IMapper mapper;
        private readonly IAbsenceRepository repository;

        public GetQueryHandler(IMapper mapper,
                               IAbsenceRepository repository)
        {
            this.mapper = mapper;
            this.repository = repository;
        }

        public async Task<AbsenceResource> Handle(GetQuery<AbsenceResource> request, CancellationToken cancellationToken)
        {
            var absence = await repository.Get(request.Id);
            return mapper.Map<Models.Schedules.Absence, AbsenceResource>(absence!);
        }
    }
}
