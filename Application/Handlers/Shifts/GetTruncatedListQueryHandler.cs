using AutoMapper;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Application.Queries.Shifts;
using Klacks.Api.Presentation.DTOs.Filter;
using MediatR;

namespace Klacks.Api.Application.Handlers.Shifts;

public class GetTruncatedListQueryHandler : IRequestHandler<GetTruncatedListQuery, TruncatedShiftResource>
{
    private readonly IMapper mapper;
    private readonly IShiftRepository shiftRepository;

    public GetTruncatedListQueryHandler(
        IMapper mapper,
        IShiftRepository shiftRepository)
    {
        this.mapper = mapper;
        this.shiftRepository = shiftRepository;
    }

    public async Task<TruncatedShiftResource> Handle(GetTruncatedListQuery request, CancellationToken cancellationToken)
    {
        var truncated = await shiftRepository.GetFilteredAndPaginatedShifts(request.Filter);

        return mapper.Map<TruncatedShift, TruncatedShiftResource>(truncated!);
    }
}