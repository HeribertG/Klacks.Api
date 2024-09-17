using AutoMapper;
using Klacks_api.Interfaces;
using Klacks_api.Queries;
using Klacks_api.Resources.Schedules;
using MediatR;

namespace Klacks_api.Handlers.Breaks
{
  public class GetQueryHandler : IRequestHandler<GetQuery<BreakResource>, BreakResource>
  {
    private readonly IMapper mapper;
    private readonly IBreakRepository repository;

    public GetQueryHandler(
                           IMapper mapper,
                           IBreakRepository repository)
    {
      this.mapper = mapper;
      this.repository = repository;
    }

    public async Task<BreakResource> Handle(GetQuery<BreakResource> request, CancellationToken cancellationToken)
    {
      var breaks = await repository.Get(request.Id);
      return mapper.Map<Models.Schedules.Break, BreakResource>(breaks!);
    }
  }
}
