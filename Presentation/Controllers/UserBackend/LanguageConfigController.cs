using Klacks.Api.Domain.Common;
using Klacks.Api.Presentation.DTOs.Config;
using Microsoft.AspNetCore.Mvc;

namespace Klacks.Api.Presentation.Controllers.UserBackend;

[ApiController]
[Route("api/config")]
public class LanguageConfigController : ControllerBase
{
    [HttpGet("languages")]
    public ActionResult<LanguageConfigResponse> GetLanguages()
    {
        return Ok(new LanguageConfigResponse
        {
            SupportedLanguages = MultiLanguage.SupportedLanguages,
            FallbackOrder = LanguageConfig.FallbackOrder
        });
    }
}
