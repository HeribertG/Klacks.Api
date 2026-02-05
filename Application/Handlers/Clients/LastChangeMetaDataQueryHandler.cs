using Klacks.Api.Application.Mappers;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Application.Queries.Clients;
using Klacks.Api.Application.DTOs.Filter;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Handlers.Clients
{
    public class LastChangeMetaDataQueryHandler : IRequestHandler<LastChangeMetaDataQuery, LastChangeMetaDataResource>
    {
        private readonly IClientRepository _clientRepository;
        private readonly SettingsMapper _settingsMapper;

        public LastChangeMetaDataQueryHandler(IClientRepository clientRepository, SettingsMapper settingsMapper)
        {
            _clientRepository = clientRepository;
            _settingsMapper = settingsMapper;
        }

        public async Task<LastChangeMetaDataResource> Handle(LastChangeMetaDataQuery request, CancellationToken cancellationToken)
        {
            var lastChangeMetaData = await _clientRepository.LastChangeMetaData();
            return _settingsMapper.ToLastChangeMetaDataResource(lastChangeMetaData);
        }
    }
}
