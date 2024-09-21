using Klacks.Api.Queries;
using Klacks.Api.Resources.Settings;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Klacks.Api.Controllers.V1.Backend;

public class CountriesController : InputBaseController<CountryResource>
{
  private readonly ILogger<CountriesController> _logger;

  public CountriesController(IMediator mediator, ILogger<CountriesController> logger)
    : base(mediator, logger)
  {
    _logger = logger;
  }

  [HttpGet]
  public async Task<ActionResult<IEnumerable<CountryResource>>> List()
  {
    try
    {
      _logger.LogInformation("Fetching list of countries.");
      var countries = await this.mediator.Send(new ListQuery<CountryResource>());
      _logger.LogInformation($"Retrieved {countries.Count()} countries.");
      return Ok(countries);
    }
    catch (Exception ex)
    {
      _logger.LogError(ex, "Error occurred while fetching list of countries.");
      throw;
    }
  }
}
