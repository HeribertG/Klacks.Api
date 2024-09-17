using AutoMapper;
using Klacks_api.Commands;
using Klacks_api.Interfaces;
using Klacks_api.Resources.Settings;
using MediatR;

namespace Klacks_api.Settings.Countries;

public class PostCommandHandler : IRequestHandler<PostCommand<CountryResource>, CountryResource?>
{
  private readonly ILogger<PostCommandHandler> logger;
  private readonly IMapper mapper;
  private readonly ICountryRepository repository;
  private readonly IUnitOfWork unitOfWork;

  public PostCommandHandler(
                            IMapper mapper,
                            ICountryRepository repository,
                            IUnitOfWork unitOfWork,
                            ILogger<PostCommandHandler> logger)
  {
    this.mapper = mapper;
    this.repository = repository;
    this.unitOfWork = unitOfWork;
    this.logger = logger;
  }

  public async Task<CountryResource?> Handle(PostCommand<CountryResource> request, CancellationToken cancellationToken)
  {
    try
    {
      var country = this.mapper.Map<CountryResource, Models.Settings.Countries>(request.Resource);

      this.repository.Add(country);

      await this.unitOfWork.CompleteAsync();

      logger.LogInformation("New country added successfully. ID: {CountryId}", country.Id);

      return this.mapper.Map<Models.Settings.Countries, CountryResource>(country);
    }
    catch (Exception ex)
    {
      logger.LogError(ex, "Error occurred while adding a new country.");
      throw;
    }
  }
}
