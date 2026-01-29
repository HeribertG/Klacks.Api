using Klacks.Api.Domain.Common;
using Klacks.Api.Presentation.DTOs.Config;
using Microsoft.AspNetCore.Mvc;

namespace Klacks.Api.Presentation.Controllers.UserBackend;

[ApiController]
[Route("api/config")]
public class LanguageConfigController : ControllerBase
{
    private readonly IConfiguration _configuration;

    public LanguageConfigController(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    [HttpGet("languages")]
    public ActionResult<LanguageConfigResponse> GetLanguages()
    {
        var languagesSection = _configuration.GetSection("Languages");

        var supported = languagesSection.GetSection("Supported").Get<string[]>();
        if (supported == null || supported.Length == 0)
            supported = LanguageConfig.SupportedLanguages;

        var fallbackOrder = languagesSection.GetSection("FallbackOrder").Get<string[]>();
        if (fallbackOrder == null || fallbackOrder.Length == 0)
            fallbackOrder = LanguageConfig.FallbackOrder;

        var metadata = languagesSection.GetSection("Metadata").Get<Dictionary<string, LanguageMetadata>>()
            ?? new Dictionary<string, LanguageMetadata>();

        return Ok(new LanguageConfigResponse
        {
            SupportedLanguages = supported,
            FallbackOrder = fallbackOrder,
            Metadata = metadata
        });
    }
}
