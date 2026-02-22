using Klacks.Api.Application.Mappers;
using Klacks.Api.Application.Commands;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Application.DTOs.Settings;
using Klacks.Api.Infrastructure.Mediator;
using Klacks.Api.Domain.Interfaces;

namespace Klacks.Api.Application.Handlers.Countries;

public class DeleteCommandHandler : BaseHandler, IRequestHandler<DeleteCommand<CountryResource>, CountryResource?>
{
    private readonly ICountryRepository _countryRepository;
    private readonly SettingsMapper _settingsMapper;
    private readonly IUnitOfWork _unitOfWork;

    public DeleteCommandHandler(
        ICountryRepository countryRepository,
        SettingsMapper settingsMapper,
        IUnitOfWork unitOfWork,
        ILogger<DeleteCommandHandler> logger)
        : base(logger)
    {
        _countryRepository = countryRepository;
        _settingsMapper = settingsMapper;
        _unitOfWork = unitOfWork;
    }

    public async Task<CountryResource?> Handle(DeleteCommand<CountryResource> request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Deleting country with ID: {CountryId}", request.Id);

        var existingCountry = await _countryRepository.Get(request.Id);
        if (existingCountry == null)
        {
            _logger.LogWarning("Country not found: {CountryId}", request.Id);
            return null;
        }

        var countryResource = _settingsMapper.ToCountryResource(existingCountry);

        await _countryRepository.Delete(request.Id);
        await _unitOfWork.CompleteAsync();

        _logger.LogInformation("Country deleted successfully: {CountryId}", request.Id);
        return countryResource;
    }
}
