using Klacks.Api.Application.Interfaces;
using Klacks.Api.Application.Queries.Settings.Vats;
using MediatR;

namespace Klacks.Api.Application.Handlers.Settings.Vat
{
    public class GetQueryHandler : IRequestHandler<GetQuery, Klacks.Api.Domain.Models.Settings.Vat?>
    {
        private readonly ISettingsRepository _settingsRepository;

        public GetQueryHandler(ISettingsRepository settingsRepository)
        {
            _settingsRepository = settingsRepository;
        }

        public async Task<Klacks.Api.Domain.Models.Settings.Vat?> Handle(GetQuery request, CancellationToken cancellationToken)
        {
            return await _settingsRepository.GetVAT(request.Id);
        }
    }
}
