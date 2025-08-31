using AutoMapper;
using Klacks.Api.Application.Commands;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Presentation.DTOs.Settings;
using MediatR;

namespace Klacks.Api.Application.Handlers.Countries;

public class DeleteCommandHandler : BaseHandler, IRequestHandler<DeleteCommand<CountryResource>, CountryResource?>
{
    private readonly ICountryRepository _countryRepository;
    private readonly IMapper _mapper;
    private readonly IUnitOfWork _unitOfWork;

    public DeleteCommandHandler(
        ICountryRepository countryRepository,
        IMapper mapper,
        IUnitOfWork unitOfWork,
        ILogger<DeleteCommandHandler> logger)
        : base(logger)
    {
        _countryRepository = countryRepository;
        _mapper = mapper;
        _unitOfWork = unitOfWork;
    }

    public async Task<CountryResource?> Handle(DeleteCommand<CountryResource> request, CancellationToken cancellationToken)
    {
        var existingCountry = await _countryRepository.Get(request.Id);
        if (existingCountry == null)
        {
            return null;
        }

        var countryResource = _mapper.Map<CountryResource>(existingCountry);
        await _countryRepository.Delete(request.Id);
        await _unitOfWork.CompleteAsync();
        return countryResource;
    }
}
