using AutoMapper;
using Klacks_api.Interfaces;
using Klacks_api.Queries;
using Klacks_api.Resources.Schedules;
using MediatR;

namespace Klacks_api.Handlers.Absences;

public class GetListQueryHandler : IRequestHandler<ListQuery<AbsenceResource>, IEnumerable<AbsenceResource>>
{
  private readonly IMapper mapper;
  private readonly IAbsenceRepository repository;

  public GetListQueryHandler(IMapper mapper, IAbsenceRepository repository)
  {
    this.mapper = mapper;
    this.repository = repository;
  }

  public async Task<IEnumerable<AbsenceResource>> Handle(ListQuery<AbsenceResource> request, CancellationToken cancellationToken)
  {
    var absence = await repository.List();
    return mapper.Map<List<Models.Schedules.Absence>, List<AbsenceResource>>(absence);
  }
}
