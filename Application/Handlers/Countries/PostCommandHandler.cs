using AutoMapper;
using Klacks.Api.Application.Commands;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Domain.Models.Settings;
using Klacks.Api.Presentation.DTOs.Settings;
using MediatR;

namespace Klacks.Api.Application.Handlers.Countries;

public class PostCommandHandler : IRequestHandler<PostCommand<CountryResource>, CountryResource?>
{
    private readonly ICountryRepository _countryRepository;
    private readonly IMapper _mapper;
    private readonly IUnitOfWork _unitOfWork;

    public PostCommandHandler(
        ICountryRepository countryRepository,
        IMapper mapper,
        IUnitOfWork unitOfWork)
    {
        _countryRepository = countryRepository;
        _mapper = mapper;
        _unitOfWork = unitOfWork;
    }

    public async Task<CountryResource?> Handle(PostCommand<CountryResource> request, CancellationToken cancellationToken)
    {
        var country = _mapper.Map<Klacks.Api.Domain.Models.Settings.Countries>(request.Resource);
        await _countryRepository.Add(country);
        await _unitOfWork.CompleteAsync();
        return _mapper.Map<CountryResource>(country);
    }
}
