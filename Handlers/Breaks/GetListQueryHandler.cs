using AutoMapper;
using Klacks.Api.Interfaces;
using Klacks.Api.Queries.Breaks;
using Klacks.Api.Presentation.Resources.Schedules;
using MediatR;

namespace Klacks.Api.Handlers.Breaks;

public class GetListQueryHandler(IMapper mapper, IClientRepository repository) : IRequestHandler<ListQuery, IEnumerable<ClientBreakResource>>
{
    private readonly IMapper mapper = mapper;
    private readonly IClientRepository repository = repository;

    public async Task<IEnumerable<ClientBreakResource>> Handle(ListQuery request, CancellationToken cancellationToken)
    {
        var client = await repository.BreakList(request.Filter);

        return mapper.Map<List<Models.Staffs.Client>, List<ClientBreakResource>>(client!);
    }
}
