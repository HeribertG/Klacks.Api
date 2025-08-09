using Klacks.Api.Application.Queries;
using Klacks.Api.Presentation.DTOs.Settings;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Klacks.Api.Presentation.Controllers.v1.UserBackend;

public class CountriesController : InputBaseController<CountryResource>
{
    private readonly ILogger<CountriesController> logger;

    public CountriesController(IMediator Mediator, ILogger<CountriesController> logger)
      : base(Mediator, logger)
    {
        this.logger = logger;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<CountryResource>>> List()
    {
        try
        {
            logger.LogInformation("Fetching list of countries.");
            var countries = await this.Mediator.Send(new ListQuery<CountryResource>());
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
