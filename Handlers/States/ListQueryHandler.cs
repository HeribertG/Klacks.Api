using AutoMapper;
using Klacks.Api.Interfaces;
using Klacks.Api.Queries;
using Klacks.Api.Resources.Settings;
using MediatR;

namespace Klacks.Api.Handlers.States;

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
