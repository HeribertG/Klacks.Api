using Microsoft.AspNetCore.Mvc;

namespace Klacks_api.Controllers.V1.Backend
{
  [ApiController]
  [Route("api/[controller]")]
  public class VersionController : ControllerBase
  {
    [HttpGet]
    public ActionResult GetVersion()
    {
      var version = new MyVersion();
      return Ok(new
      {
        Variant = MyVersion.Variant,
        Year = MyVersion.Year,
        Week = MyVersion.Week,
        Build = MyVersion.Build,
        BuildKey = MyVersion.BuildKey,
        BuildTimestamp = MyVersion.BuildTimestamp,
        VersionString = version.Get(),
        VersionStringWithBuildInfo = version.Get(true),
      });
    }
  }
}
