using AutoMapper;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Application.Queries.Shifts;
using Klacks.Api.Presentation.DTOs.Schedules;
using MediatR;

namespace Klacks.Api.Application.Handlers.Shifts;

public class CutListQueryhandler : IRequestHandler<CutListQuery, IEnumerable<ShiftResource>>
{
    private readonly IShiftRepository _shiftRepository;
    private readonly IMapper _mapper;

    public CutListQueryhandler(IShiftRepository shiftRepository, IMapper mapper)
    {
        _shiftRepository = shiftRepository;
        _mapper = mapper;
    }

    public async Task<IEnumerable<ShiftResource>> Handle(CutListQuery request, CancellationToken cancellationToken)
    {
        var cuts = await _shiftRepository.CutList(request.Id);
        return _mapper.Map<IEnumerable<ShiftResource>>(cuts);
    }
}
