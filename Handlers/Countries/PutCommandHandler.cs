using AutoMapper;
using Klacks.Api.Commands;
using Klacks.Api.Interfaces;
using Klacks.Api.Resources.Settings;
using MediatR;

namespace Klacks.Api.Settings.Countries;

public class PutCommandHandler : IRequestHandler<PutCommand<CountryResource>, CountryResource?>
{
  private readonly ILogger<PutCommandHandler> logger;
  private readonly IMapper mapper;
  private readonly ICountryRepository repository;
  private readonly IUnitOfWork unitOfWork;

  public PutCommandHandler(
                            IMapper mapper,
                            ICountryRepository repository,
                            IUnitOfWork unitOfWork,
                            ILogger<PutCommandHandler> logger)
  {
    this.mapper = mapper;
    this.repository = repository;
    this.unitOfWork = unitOfWork;
    this.logger = logger;
  }

  public async Task<CountryResource?> Handle(PutCommand<CountryResource> request, CancellationToken cancellationToken)
  {
    try
    {
      var dbCountry = await this.repository.Get(request.Resource.Id);
      if (dbCountry == null)
      {
        logger.LogWarning("Country with ID {CountryId} not found.", request.Resource.Id);
        return null;
      }

      var updatedCountry = this.mapper.Map(request.Resource, dbCountry);
      updatedCountry = await this.repository.Put(updatedCountry);
      await this.unitOfWork.CompleteAsync();

      logger.LogInformation("Country with ID {CountryId} updated successfully.", request.Resource.Id);

      return this.mapper.Map<Models.Settings.Countries, CountryResource>(updatedCountry);
    }
    catch (Exception ex)
    {
      logger.LogError(ex, "Error occurred while updating country with ID {CountryId}.", request.Resource.Id);
      throw;
    }
  }
}
