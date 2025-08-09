using AutoMapper;
using Klacks.Api.Application.Commands;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Presentation.DTOs.Settings;
using MediatR;

namespace Klacks.Api.Application.Handlers.Countries;

public class DeleteCommandHandler : IRequestHandler<DeleteCommand<CountryResource>, CountryResource?>
{
    private readonly ILogger<DeleteCommandHandler> logger;
    private readonly IMapper mapper;
    private readonly ICountryRepository repository;
    private readonly IUnitOfWork unitOfWork;

    public DeleteCommandHandler(
                                IMapper mapper,
                                ICountryRepository repository,
                                IUnitOfWork unitOfWork,
                                ILogger<DeleteCommandHandler> logger)
    {
        this.mapper = mapper;
        this.repository = repository;
        this.unitOfWork = unitOfWork;
        this.logger = logger;
    }

    public async Task<CountryResource?> Handle(DeleteCommand<CountryResource> request, CancellationToken cancellationToken)
    {
        try
        {
            var country = await this.repository.Delete(request.Id);
            if (country == null)
            {
                logger.LogWarning("Country with ID {CountryId} not found for deletion.", request.Id);
                return null;
            }

            await this.unitOfWork.CompleteAsync();

            logger.LogInformation("Country with ID {CountryId} deleted successfully.", request.Id);

            return this.mapper.Map<Models.Settings.Countries, CountryResource>(country);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error occurred while deleting country with ID {CountryId}.", request.Id);
            throw;
        }
    }
}
