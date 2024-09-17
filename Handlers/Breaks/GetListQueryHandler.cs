using AutoMapper;
using Klacks_api.Interfaces;
using Klacks_api.Queries.Breaks;
using Klacks_api.Resources.Schedules;
using MediatR;

namespace Klacks_api.Handlers.Breaks
{
  public class GetListQueryHandler : IRequestHandler<ListQuery, IEnumerable<ClientBreakResource>>
  {
    private readonly IMapper mapper;
    private readonly IClientRepository repository;

    public GetListQueryHandler(IMapper mapper, IClientRepository repository)
    {
      this.mapper = mapper;
      this.repository = repository;
    }

    public async Task<IEnumerable<ClientBreakResource>> Handle(ListQuery request, CancellationToken cancellationToken)
    {
      var client = await repository.BreakList(request.Filter);

      return mapper.Map<List<Models.Staffs.Client>, List<ClientBreakResource>>(client!);
    }
  }
}
