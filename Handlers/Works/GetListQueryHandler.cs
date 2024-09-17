using AutoMapper;
using Klacks_api.Interfaces;
using Klacks_api.Queries.Works;
using Klacks_api.Resources.Schedules;
using MediatR;

namespace Klacks_api.Handlers.Works;

public class GetListQueryHandler : IRequestHandler<ListQuery, IEnumerable<ClientWorkResource>>
{
  private readonly IMapper mapper;
  private readonly IClientRepository repository;

  public GetListQueryHandler(IMapper mapper, IClientRepository repository)
  {
    this.mapper = mapper;
    this.repository = repository;
  }

  public async Task<IEnumerable<ClientWorkResource>> Handle(ListQuery request, CancellationToken cancellationToken)
  {
    var client = await repository.WorkList(request.Filter);

    return mapper.Map<List<Models.Staffs.Client>, List<ClientWorkResource>>(client!);
  }
}
