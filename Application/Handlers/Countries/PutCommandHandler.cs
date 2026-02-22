using Klacks.Api.Application.Mappers;
using Klacks.Api.Application.Commands;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Application.DTOs.Settings;
using Klacks.Api.Infrastructure.Mediator;
using Klacks.Api.Domain.Interfaces;

namespace Klacks.Api.Application.Handlers.Countries;

public class PutCommandHandler : BaseHandler, IRequestHandler<PutCommand<CountryResource>, CountryResource?>
{
    private readonly ICountryRepository _countryRepository;
    private readonly SettingsMapper _settingsMapper;
    private readonly IUnitOfWork _unitOfWork;

    public PutCommandHandler(
        ICountryRepository countryRepository,
        SettingsMapper settingsMapper,
        IUnitOfWork unitOfWork,
        ILogger<PutCommandHandler> logger)
        : base(logger)
    {
        _countryRepository = countryRepository;
        _settingsMapper = settingsMapper;
        _unitOfWork = unitOfWork;
    }

    public async Task<CountryResource?> Handle(PutCommand<CountryResource> request, CancellationToken cancellationToken)
    {
        var existingCountry = await _countryRepository.Get(request.Resource.Id);
        if (existingCountry == null)
        {
            return null;
        }

        _settingsMapper.UpdateCountryEntity(request.Resource, existingCountry);
        await _unitOfWork.CompleteAsync();
        return _settingsMapper.ToCountryResource(existingCountry);
    }
}
