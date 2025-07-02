using Klacks.Api.Queries;
using Klacks.Api.Resources.Settings;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Klacks.Api.Controllers.V1.UserBackend;

public class CountriesController : InputBaseController<CountryResource>
{
    private readonly ILogger<CountriesController> logger;

    public CountriesController(IMediator mediator, ILogger<CountriesController> logger)
      : base(mediator, logger)
    {
        this.logger = logger;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<CountryResource>>> List()
    {
        try
        {
            logger.LogInformation("Fetching list of countries.");
            var countries = await this.mediator.Send(new ListQuery<CountryResource>());
            logger.LogInformation($"Retrieved {countries.Count()} countries.");
            return Ok(countries);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error occurred while fetching list of countries.");
            throw;
        }
    }
}
