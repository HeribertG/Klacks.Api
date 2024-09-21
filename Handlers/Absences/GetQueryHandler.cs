using AutoMapper;
using Klacks.Api.Interfaces;
using Klacks.Api.Queries;
using Klacks.Api.Resources.Schedules;
using MediatR;

namespace Klacks.Api.Handlers.Absences
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
