using AutoMapper;
using Klacks.Api.Interfaces;
using Klacks.Api.Application.Queries.Settings.MacrosTypes;
using MediatR;

namespace Klacks.Api.Application.Handlers.Settings.MacrosTypes;

public class ListQueryHandler : IRequestHandler<ListQuery, IEnumerable<Models.Settings.MacroType>>
{
    private readonly IMapper mapper;
    private readonly ISettingsRepository repository;

    public ListQueryHandler(IMapper mapper, ISettingsRepository repository)
    {
        this.mapper = mapper;
        this.repository = repository;
    }

    public async Task<IEnumerable<Models.Settings.MacroType>> Handle(ListQuery request, CancellationToken cancellationToken)
    {
        return await repository.GetOriginalMacroTypeList();
    }
}
