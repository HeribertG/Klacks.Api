using AutoMapper;
using Klacks.Api.Interfaces;
using Klacks.Api.Models.Schedules;
using Klacks.Api.Application.Queries.Shifts;
using Klacks.Api.Presentation.DTOs.Schedules;
using MediatR;

namespace Klacks.Api.Handlers.Shifts;

public class CutListQueryhandler : IRequestHandler<CutListQuery, IEnumerable<ShiftResource>>
{
    private readonly IMapper mapper;
    private readonly IShiftRepository repository;

    public CutListQueryhandler(IMapper mapper,
                               IShiftRepository repository)
    {
        this.mapper = mapper;
        this.repository = repository;
    }

    public async Task<IEnumerable<ShiftResource>> Handle(CutListQuery request, CancellationToken cancellationToken)
    {
        var lists = await repository.CutList(request.Id);
        return mapper.Map<List<Shift>, List<ShiftResource>>(lists);
    }
}
