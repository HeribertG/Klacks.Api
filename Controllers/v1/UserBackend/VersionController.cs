using Microsoft.AspNetCore.Mvc;

namespace Klacks.Api.Controllers.V1.UserBackend
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
                MyVersion.Variant,
                MyVersion.Year,
                MyVersion.Week,
                MyVersion.Build,
                MyVersion.BuildKey,
                MyVersion.BuildTimestamp,
                rsionString = version.Get(),
                VersionStringWithBuildInfo = version.Get(true),
            });
        }
    }
}
