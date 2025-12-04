using Klacks.Api.Application.Queries;
using Klacks.Api.Presentation.DTOs.Settings;
using Klacks.Api.Infrastructure.Mediator;
using Microsoft.AspNetCore.Mvc;

namespace Klacks.Api.Presentation.Controllers.v1.UserBackend;

public class CountriesController : InputBaseController<CountryResource>
{
    public CountriesController(IMediator Mediator, ILogger<CountriesController> logger)
      : base(Mediator, logger)
    {
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<CountryResource>>> List()
    {
        var countries = await this.Mediator.Send(new ListQuery<CountryResource>());
        return Ok(countries);
    }
}
