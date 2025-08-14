using AutoMapper;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Application.Queries.Settings.MacrosTypes;
using MediatR;

namespace Klacks.Api.Application.Handlers.Settings.MacrosTypes;

public class GetQueryHandler : IRequestHandler<GetQuery, Klacks.Api.Domain.Models.Settings.MacroType?>
{
    private readonly ISettingsRepository _settingsRepository;

    public GetQueryHandler(ISettingsRepository settingsRepository)
    {
        _settingsRepository = settingsRepository;
    }

    public async Task<Klacks.Api.Domain.Models.Settings.MacroType?> Handle(GetQuery request, CancellationToken cancellationToken)
    {
        return await _settingsRepository.GetMacroType(request.Id);
    }
}
