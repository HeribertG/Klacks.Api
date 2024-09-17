using AutoMapper;
using Klacks_api.Interfaces;
using Klacks_api.Queries;
using Klacks_api.Resources.Schedules;
using MediatR;

namespace Klacks_api.Handlers.Shifts;

public class ListQueryHandler : IRequestHandler<ListQuery<ShiftResource>, IEnumerable<ShiftResource>>
{
  private readonly IMapper mapper;
  private readonly IShiftRepository repository;

  public ListQueryHandler(IMapper mapper, IShiftRepository repository)
  {
    this.mapper = mapper;
    this.repository = repository;
  }

  public async Task<IEnumerable<ShiftResource>> Handle(ListQuery<ShiftResource> request, CancellationToken cancellationToken)
  {
    var shift = await this.repository.List();
    return this.mapper.Map<List<Models.Schedules.Shift>, List<ShiftResource>>(shift!);
  }
}
