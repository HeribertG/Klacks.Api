using AutoMapper;
using Klacks.Api.Interfaces;
using Klacks.Api.Queries.Shifts;
using Klacks.Api.Resources.Filter;
using MediatR;

namespace Klacks.Api.Handlers.Shifts;

public class GetTruncatedListQueryHandler : IRequestHandler<GetTruncatedListQuery, TruncatedShiftResource>
{
    private readonly IMapper mapper;
    private readonly IShiftRepository repository;

    public GetTruncatedListQueryHandler(
                                        IMapper mapper,
                                        IShiftRepository repository)
    {
        this.mapper = mapper;
        this.repository = repository;
    }

    public async Task<TruncatedShiftResource> Handle(GetTruncatedListQuery request, CancellationToken cancellationToken)
    {
        var truncated = await repository.Truncated(request.Filter);
        return mapper.Map<TruncatedShift, TruncatedShiftResource>(truncated!);
    }
}