using Klacks.Api.Application.Interfaces;
using Klacks.Api.Application.Queries.Settings.Vats;
using MediatR;

namespace Klacks.Api.Application.Handlers.Settings.Vat
{
    public class ListQueryHandler : IRequestHandler<ListQuery, IEnumerable<Klacks.Api.Domain.Models.Settings.Vat>>
    {
        private readonly ISettingsRepository _settingsRepository;

        public ListQueryHandler(ISettingsRepository settingsRepository)
        {
            _settingsRepository = settingsRepository;
        }

        public async Task<IEnumerable<Klacks.Api.Domain.Models.Settings.Vat>> Handle(ListQuery request, CancellationToken cancellationToken)
        {
            return await _settingsRepository.GetVATList();
        }
    }
}
