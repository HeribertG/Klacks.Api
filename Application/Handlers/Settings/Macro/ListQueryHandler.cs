using AutoMapper;
using Klacks.Api.Application.Services;
using Klacks.Api.Application.Queries.Settings.Macros;
using Klacks.Api.Presentation.DTOs.Settings;
using MediatR;

namespace Klacks.Api.Application.Handlers.Settings.Macros;

public class ListQueryHandler : IRequestHandler<ListQuery, IEnumerable<MacroResource>>
{
    private readonly SettingsApplicationService _settingsApplicationService;

    public ListQueryHandler(SettingsApplicationService settingsApplicationService)
    {
        _settingsApplicationService = settingsApplicationService;
    }

    public async Task<IEnumerable<MacroResource>> Handle(ListQuery request, CancellationToken cancellationToken)
    {
        return await _settingsApplicationService.GetAllMacrosAsync(cancellationToken);
    }
}
