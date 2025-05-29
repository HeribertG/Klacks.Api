using AutoMapper;
using Klacks.Api.Interfaces;
using Klacks.Api.Queries.Works;
using Klacks.Api.Resources.Schedules;
using MediatR;

namespace Klacks.Api.Handlers.Works;

public class GetListQueryHandler : IRequestHandler<ListQuery, IEnumerable<ClientWorkResource>>
{
    private readonly IMapper mapper;
    private readonly IClientRepository repository;

    public GetListQueryHandler(IMapper mapper, IClientRepository repository)
    {
        this.mapper = mapper;
        this.repository = repository;
    }

    public async Task<IEnumerable<ClientWorkResource>> Handle(ListQuery request, CancellationToken cancellationToken)
    {
        var client = await repository.WorkList(request.Filter);

        return mapper.Map<List<Models.Staffs.Client>, List<ClientWorkResource>>(client!);
    }
}
