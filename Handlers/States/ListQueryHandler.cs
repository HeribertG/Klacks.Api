using AutoMapper;
using Klacks_api.Interfaces;
using Klacks_api.Queries;
using Klacks_api.Resources.Settings;
using MediatR;

namespace Klacks_api.Handlers.States;

public class ListQueryHandler : IRequestHandler<ListQuery<StateResource>, IEnumerable<StateResource>>
{
  private readonly IMapper mapper;
  private readonly IStateRepository repository;

  public ListQueryHandler(IMapper mapper, IStateRepository repository)
  {
    this.mapper = mapper;
    this.repository = repository;
  }

  public async Task<IEnumerable<StateResource>> Handle(ListQuery<StateResource> request, CancellationToken cancellationToken)
  {
    var states = await this.repository.List();
    return this.mapper.Map<List<Models.Settings.State>, List<StateResource>>(states!);
  }
}
