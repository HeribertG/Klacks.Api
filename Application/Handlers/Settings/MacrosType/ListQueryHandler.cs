using AutoMapper;
using Klacks.Api.Application.Services;
using Klacks.Api.Application.Queries.Settings.MacrosTypes;
using MediatR;

namespace Klacks.Api.Application.Handlers.Settings.MacrosTypes;

public class ListQueryHandler : IRequestHandler<ListQuery, IEnumerable<Klacks.Api.Domain.Models.Settings.MacroType>>
{
    private readonly SettingsApplicationService _settingsApplicationService;

    public ListQueryHandler(SettingsApplicationService settingsApplicationService)
    {
        _settingsApplicationService = settingsApplicationService;
    }

    public async Task<IEnumerable<Klacks.Api.Domain.Models.Settings.MacroType>> Handle(ListQuery request, CancellationToken cancellationToken)
    {
        return await _settingsApplicationService.GetAllMacroTypesAsync(cancellationToken);
    }
}
