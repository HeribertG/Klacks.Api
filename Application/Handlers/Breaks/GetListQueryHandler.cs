using AutoMapper;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Application.Queries.Breaks;
using Klacks.Api.Presentation.DTOs.Schedules;
using MediatR;

namespace Klacks.Api.Application.Handlers.Breaks;

public class GetListQueryHandler(IMapper mapper, IClientRepository repository) : IRequestHandler<ListQuery, IEnumerable<ClientBreakResource>>
{
    private readonly IMapper mapper = mapper;
    private readonly IClientRepository repository = repository;

    public async Task<IEnumerable<ClientBreakResource>> Handle(ListQuery request, CancellationToken cancellationToken)
    {
        var client = await repository.BreakList(request.Filter);

        return mapper.Map<List<Klacks.Api.Domain.Models.Staffs.Client>, List<ClientBreakResource>>(client!);
    }
}
