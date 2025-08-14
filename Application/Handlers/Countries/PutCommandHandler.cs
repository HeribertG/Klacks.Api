using AutoMapper;
using Klacks.Api.Application.Commands;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Presentation.DTOs.Settings;
using MediatR;

namespace Klacks.Api.Application.Handlers.Countries;

public class PutCommandHandler : IRequestHandler<PutCommand<CountryResource>, CountryResource?>
{
    private readonly ICountryRepository _countryRepository;
    private readonly IMapper _mapper;
    private readonly IUnitOfWork _unitOfWork;

    public PutCommandHandler(
        ICountryRepository countryRepository,
        IMapper mapper,
        IUnitOfWork unitOfWork)
    {
        _countryRepository = countryRepository;
        _mapper = mapper;
        _unitOfWork = unitOfWork;
    }

    public async Task<CountryResource?> Handle(PutCommand<CountryResource> request, CancellationToken cancellationToken)
    {
        var existingCountry = await _countryRepository.Get(request.Resource.Id);
        if (existingCountry == null)
        {
            return null;
        }

        _mapper.Map(request.Resource, existingCountry);
        await _countryRepository.Put(existingCountry);
        await _unitOfWork.CompleteAsync();
        return _mapper.Map<CountryResource>(existingCountry);
    }
}
