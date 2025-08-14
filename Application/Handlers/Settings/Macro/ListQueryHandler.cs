using AutoMapper;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Application.Queries.Settings.Macros;
using Klacks.Api.Presentation.DTOs.Settings;
using MediatR;

namespace Klacks.Api.Application.Handlers.Settings.Macros;

public class ListQueryHandler : IRequestHandler<ListQuery, IEnumerable<MacroResource>>
{
    private readonly ISettingsRepository _settingsRepository;
    private readonly IMapper _mapper;

    public ListQueryHandler(ISettingsRepository settingsRepository, IMapper mapper)
    {
        _settingsRepository = settingsRepository;
        _mapper = mapper;
    }

    public async Task<IEnumerable<MacroResource>> Handle(ListQuery request, CancellationToken cancellationToken)
    {
        var macros = await _settingsRepository.GetMacroList();
        return _mapper.Map<IEnumerable<MacroResource>>(macros);
    }
}
