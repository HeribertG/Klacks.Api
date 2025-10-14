using AutoMapper;
using Klacks.Api.Application.Commands;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Infrastructure.Persistence;
using Klacks.Api.Presentation.DTOs.Settings;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Klacks.Api.Application.Handlers.Countries;

public class DeleteCommandHandler : BaseHandler, IRequestHandler<DeleteCommand<CountryResource>, CountryResource?>
{
    private readonly ICountryRepository _countryRepository;
    private readonly IMapper _mapper;
    private readonly DataBaseContext _context;

    public DeleteCommandHandler(
        ICountryRepository countryRepository,
        IMapper mapper,
        DataBaseContext context,
        ILogger<DeleteCommandHandler> logger)
        : base(logger)
    {
        _countryRepository = countryRepository;
        _mapper = mapper;
        _context = context;
    }

    public async Task<CountryResource?> Handle(DeleteCommand<CountryResource> request, CancellationToken cancellationToken)
    {
        var existingCountry = await _countryRepository.Get(request.Id);
        if (existingCountry == null)
        {
            return null;
        }

        var countryResource = _mapper.Map<CountryResource>(existingCountry);

        await _context.Countries.Where(c => c.Id == request.Id).ExecuteDeleteAsync(cancellationToken);

        return countryResource;
    }
}
