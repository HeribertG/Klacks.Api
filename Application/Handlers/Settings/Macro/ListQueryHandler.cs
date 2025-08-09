using AutoMapper;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Application.Queries.Settings.Macros;
using Klacks.Api.Presentation.DTOs.Settings;
using MediatR;

namespace Klacks.Api.Application.Handlers.Settings.Macros;

public class ListQueryHandler : IRequestHandler<ListQuery, IEnumerable<MacroResource>>
{
    private readonly IMapper mapper;
    private readonly ISettingsRepository repository;

    public ListQueryHandler(IMapper mapper, ISettingsRepository repository)
    {
        this.mapper = mapper;
        this.repository = repository;
    }

    public async Task<IEnumerable<MacroResource>> Handle(ListQuery request, CancellationToken cancellationToken)
    {
        var macro = await repository.GetMacroList();
        return mapper.Map<List<Models.Settings.Macro>, List<MacroResource>>(macro);
    }
}
