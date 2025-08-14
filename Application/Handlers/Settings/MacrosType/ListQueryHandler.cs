using AutoMapper;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Application.Queries.Settings.MacrosTypes;
using MediatR;

namespace Klacks.Api.Application.Handlers.Settings.MacrosTypes;

public class ListQueryHandler : IRequestHandler<ListQuery, IEnumerable<Klacks.Api.Domain.Models.Settings.MacroType>>
{
    private readonly ISettingsRepository _settingsRepository;

    public ListQueryHandler(ISettingsRepository settingsRepository)
    {
        _settingsRepository = settingsRepository;
    }

    public async Task<IEnumerable<Klacks.Api.Domain.Models.Settings.MacroType>> Handle(ListQuery request, CancellationToken cancellationToken)
    {
        return await _settingsRepository.GetOriginalMacroTypeList();
    }
}
