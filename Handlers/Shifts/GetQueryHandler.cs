using AutoMapper;
using Klacks_api.Interfaces;
using Klacks_api.Queries;
using Klacks_api.Resources.Schedules;
using MediatR;

namespace Klacks_api.Handlers.Shifts;

public class GetQueryHandler : IRequestHandler<GetQuery<ShiftResource>, ShiftResource>
{
  private readonly IMapper mapper;
  private readonly IShiftRepository repository;

  public GetQueryHandler(
                         IMapper mapper,
                         IShiftRepository repository)
  {
    this.mapper = mapper;
    this.repository = repository;
  }

  public async Task<ShiftResource> Handle(GetQuery<ShiftResource> request, CancellationToken cancellationToken)
  {
    var shift = await repository.Get(request.Id);
    return mapper.Map<Models.Schedules.Shift, ShiftResource>(shift!);
  }
}
