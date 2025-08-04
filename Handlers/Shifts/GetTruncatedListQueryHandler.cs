using AutoMapper;
using Klacks.Api.Interfaces.Domains;
using Klacks.Api.Queries.Shifts;
using Klacks.Api.Resources.Filter;
using MediatR;

namespace Klacks.Api.Handlers.Shifts;

public class GetTruncatedListQueryHandler : IRequestHandler<GetTruncatedListQuery, TruncatedShiftResource>
{
    private readonly IMapper mapper;
    private readonly IShiftFilterService shiftFilterService;

    public GetTruncatedListQueryHandler(
        IMapper mapper,
        IShiftFilterService shiftFilterService)
    {
        this.mapper = mapper;
        this.shiftFilterService = shiftFilterService;
    }

    public async Task<TruncatedShiftResource> Handle(GetTruncatedListQuery request, CancellationToken cancellationToken)
    {
        var truncated = await shiftFilterService.GetFilteredAndPaginatedShifts(request.Filter);

        return mapper.Map<TruncatedShift, TruncatedShiftResource>(truncated!);
    }
}