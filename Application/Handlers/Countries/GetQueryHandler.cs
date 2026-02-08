using Klacks.Api.Application.Mappers;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Application.Queries;
using Klacks.Api.Application.DTOs.Settings;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Handlers.Countries
{
    public class GetQueryHandler : BaseHandler, IRequestHandler<GetQuery<CountryResource>, CountryResource>
    {
        private readonly ICountryRepository _countryRepository;
        private readonly SettingsMapper _settingsMapper;

        public GetQueryHandler(ICountryRepository countryRepository, SettingsMapper settingsMapper, ILogger<GetQueryHandler> logger)
            : base(logger)
        {
            _countryRepository = countryRepository;
            _settingsMapper = settingsMapper;
        }

        public async Task<CountryResource> Handle(GetQuery<CountryResource> request, CancellationToken cancellationToken)
        {
            return await ExecuteAsync(async () =>
            {
                var country = await _countryRepository.Get(request.Id);

                if (country == null)
                {
                    throw new KeyNotFoundException($"Country with ID {request.Id} not found");
                }

                return _settingsMapper.ToCountryResource(country);
            }, nameof(Handle), new { request.Id });
        }
    }
}
