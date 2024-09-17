using AutoMapper;
using Klacks_api.Interfaces;
using Klacks_api.Queries.Absences;
using Klacks_api.Resources.Filter;
using MediatR;

namespace Klacks_api.Handlers.Absences
{
  public class GetTruncatedListQueryHandler : IRequestHandler<TruncatedListQuery, TruncatedAbsence>
  {
    private readonly IMapper mapper;
    private readonly IAbsenceRepository repository;

    public GetTruncatedListQueryHandler(IMapper mapper,
                                        IAbsenceRepository repository)
    {
      this.mapper = mapper;
      this.repository = repository;
    }

    public async Task<TruncatedAbsence> Handle(TruncatedListQuery request, CancellationToken cancellationToken)
    {
      var tmp = await repository.Truncated(request.Filter);
      return mapper.Map<TruncatedAbsence_dto, TruncatedAbsence>(tmp!);
    }
  }
}
