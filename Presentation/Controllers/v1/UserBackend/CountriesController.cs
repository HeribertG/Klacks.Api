using Klacks.Api.Application.Queries;
using Klacks.Api.Presentation.DTOs.Settings;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Klacks.Api.Presentation.Controllers.v1.UserBackend;

public class CountriesController : InputBaseController<CountryResource>
{
    private readonly ILogger<CountriesController> _logger;

    public CountriesController(IMediator Mediator, ILogger<CountriesController> logger)
      : base(Mediator, logger)
    {
        this._logger = logger;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<CountryResource>>> List()
    {
        _logger.LogInformation("Fetching list of countries.");
        var countries = await this.Mediator.Send(new ListQuery<CountryResource>());
        _logger.LogInformation($"Retrieved {countries.Count()} countries.");
        return Ok(countries);
    }
}
