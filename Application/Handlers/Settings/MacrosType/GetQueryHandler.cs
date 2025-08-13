using AutoMapper;
using Klacks.Api.Application.Services;
using Klacks.Api.Application.Queries.Settings.MacrosTypes;
using MediatR;

namespace Klacks.Api.Application.Handlers.Settings.MacrosTypes;

public class GetQueryHandler : IRequestHandler<GetQuery, Klacks.Api.Domain.Models.Settings.MacroType?>
{
    private readonly SettingsApplicationService _settingsApplicationService;

    public GetQueryHandler(SettingsApplicationService settingsApplicationService)
    {
        _settingsApplicationService = settingsApplicationService;
    }

    public async Task<Klacks.Api.Domain.Models.Settings.MacroType?> Handle(GetQuery request, CancellationToken cancellationToken)
    {
        return await _settingsApplicationService.GetMacroTypeByIdAsync(request.Id, cancellationToken);
    }
}
