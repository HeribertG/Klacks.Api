// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Application.Mappers;
using Klacks.Api.Application.Commands;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Application.DTOs.Settings;
using Klacks.Api.Infrastructure.Mediator;
using Klacks.Api.Domain.Interfaces;

namespace Klacks.Api.Application.Handlers.Countries;

public class PostCommandHandler : BaseHandler, IRequestHandler<PostCommand<CountryResource>, CountryResource?>
{
    private readonly ICountryRepository _countryRepository;
    private readonly SettingsMapper _settingsMapper;
    private readonly IUnitOfWork _unitOfWork;

    public PostCommandHandler(
        ICountryRepository countryRepository,
        SettingsMapper settingsMapper,
        IUnitOfWork unitOfWork,
        ILogger<PostCommandHandler> logger)
        : base(logger)
    {
        _countryRepository = countryRepository;
        _settingsMapper = settingsMapper;
        _unitOfWork = unitOfWork;
    }

    public async Task<CountryResource?> Handle(PostCommand<CountryResource> request, CancellationToken cancellationToken)
    {
        var country = _settingsMapper.ToCountryEntity(request.Resource);
        await _countryRepository.Add(country);
        await _unitOfWork.CompleteAsync();
        return _settingsMapper.ToCountryResource(country);
    }
}
