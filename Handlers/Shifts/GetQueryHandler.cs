using AutoMapper;
using Klacks.Api.Interfaces;
using Klacks.Api.Queries;
using Klacks.Api.Resources.Schedules;
using MediatR;

namespace Klacks.Api.Handlers.Shifts;

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
