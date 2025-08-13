using AutoMapper;
using Klacks.Api.Application.Services;
using Klacks.Api.Application.Queries.Settings.Macros;
using Klacks.Api.Presentation.DTOs.Settings;
using MediatR;

namespace Klacks.Api.Application.Handlers.Settings.Macro
{
    public class GetQueryHandler : IRequestHandler<GetQuery, MacroResource?>
    {
        private readonly SettingsApplicationService _settingsApplicationService;

        public GetQueryHandler(SettingsApplicationService settingsApplicationService)
        {
            _settingsApplicationService = settingsApplicationService;
        }

        public async Task<MacroResource?> Handle(GetQuery request, CancellationToken cancellationToken)
        {
            return await _settingsApplicationService.GetMacroByIdAsync(request.Id, cancellationToken);
        }
    }
}
