using AutoMapper;
using Klacks.Api.Interfaces;
using Klacks.Api.Queries.Breaks;
using Klacks.Api.Resources.Schedules;
using MediatR;

namespace Klacks.Api.Handlers.Breaks
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
