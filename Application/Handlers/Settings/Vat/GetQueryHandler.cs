using Klacks.Api.Application.Interfaces;
using Klacks.Api.Application.Queries.Settings.Vats;
using Klacks.Api.Application.Services;
using MediatR;

namespace Klacks.Api.Application.Handlers.Settings.Vat
{
    public class GetQueryHandler : IRequestHandler<GetQuery, Klacks.Api.Domain.Models.Settings.Vat?>
    {
        private readonly SettingsApplicationService _settingsApplicationService;

        public GetQueryHandler(SettingsApplicationService settingsApplicationService)
        {
            _settingsApplicationService = settingsApplicationService;
        }

        public async Task<Klacks.Api.Domain.Models.Settings.Vat?> Handle(GetQuery request, CancellationToken cancellationToken)
        {
            return await _settingsApplicationService.GetVatByIdAsync(request.Id, cancellationToken);
        }
    }
}
