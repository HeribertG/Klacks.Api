using AutoMapper;
using Klacks_api.Interfaces;
using Klacks_api.Queries;
using Klacks_api.Resources.Schedules;
using MediatR;

namespace Klacks_api.Handlers.Works;

public class GetQueryHandler : IRequestHandler<GetQuery<WorkResource>, WorkResource>
{
  private readonly IMapper mapper;
  private readonly IWorkRepository repository;

  public GetQueryHandler(IMapper mapper,
                         IWorkRepository repository)
  {
    this.mapper = mapper;
    this.repository = repository;
  }

  public async Task<WorkResource> Handle(GetQuery<WorkResource> request, CancellationToken cancellationToken)
  {
    var work = await repository.Get(request.Id);
    return mapper.Map<Models.Schedules.Work, WorkResource>(work!);
  }
}
