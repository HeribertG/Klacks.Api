using AutoMapper;
using Klacks.Api.Interfaces;
using Klacks.Api.Application.Queries;
using Klacks.Api.Presentation.DTOs.Settings;
using MediatR;

namespace Klacks.Api.Application.Handlers.States;

public class ListQueryHandler : IRequestHandler<ListQuery<StateResource>, IEnumerable<StateResource>>
{
    private readonly IMapper mapper;
    private readonly IStateRepository repository;

    public ListQueryHandler(IMapper mapper, IStateRepository repository)
    {
        this.mapper = mapper;
        this.repository = repository;
    }

    public async Task<IEnumerable<StateResource>> Handle(ListQuery<StateResource> request, CancellationToken cancellationToken)
    {
        var states = await this.repository.List();
        return this.mapper.Map<List<Models.Settings.State>, List<StateResource>>(states!);
    }
}
