using AutoMapper;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Application.Queries.Shifts;
using Klacks.Api.Presentation.DTOs.Filter;
using MediatR;

namespace Klacks.Api.Application.Handlers.Shifts;

public class GetTruncatedListQueryHandler : IRequestHandler<GetTruncatedListQuery, TruncatedShiftResource>
{
    private readonly IShiftRepository _shiftRepository;
    private readonly IMapper _mapper;

    public GetTruncatedListQueryHandler(IShiftRepository shiftRepository, IMapper mapper)
    {
        _shiftRepository = shiftRepository;
        _mapper = mapper;
    }

    public async Task<TruncatedShiftResource> Handle(GetTruncatedListQuery request, CancellationToken cancellationToken)
    {
        var truncatedShift = await _shiftRepository.GetFilteredAndPaginatedShifts(request.Filter);
        return _mapper.Map<TruncatedShiftResource>(truncatedShift);
    }
}