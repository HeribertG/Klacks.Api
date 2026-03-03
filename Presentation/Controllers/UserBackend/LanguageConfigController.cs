// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Domain.Common;
using Klacks.Api.Application.DTOs.Config;
using Klacks.Api.Domain.Interfaces.Settings;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Klacks.Api.Presentation.Controllers.UserBackend;

[ApiController]
[Route("api/config")]
public class LanguageConfigController : ControllerBase
{
    private readonly IConfiguration _configuration;
    private readonly ILanguagePluginService _languagePluginService;

    public LanguageConfigController(
        IConfiguration configuration,
        ILanguagePluginService languagePluginService)
    {
        _configuration = configuration;
        _languagePluginService = languagePluginService;
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

        var installedPluginCodes = _languagePluginService.GetInstalledPluginCodes();
        var allSupported = supported.ToList();

        foreach (var code in installedPluginCodes)
        {
            if (!allSupported.Contains(code))
            {
                allSupported.Add(code);

                var plugin = _languagePluginService.GetPlugin(code);
                if (plugin != null && !metadata.ContainsKey(code))
                {
                    metadata[code] = new LanguageMetadata
                    {
                        Name = plugin.Name,
                        DisplayName = plugin.DisplayName,
                        SpeechLocale = plugin.SpeechLocale
                    };
                }
            }
        }

        return Ok(new LanguageConfigResponse
        {
            SupportedLanguages = allSupported.ToArray(),
            FallbackOrder = fallbackOrder,
            Metadata = metadata
        });
    }

    [HttpGet("translations/{lang}")]
    public ActionResult<Dictionary<string, string>> GetTranslations(string lang)
    {
        var translations = _languagePluginService.GetTranslations(lang);
        if (translations == null)
            return NotFound();

        return Ok(translations);
    }

    [HttpGet("language-plugins")]
    public ActionResult<IReadOnlyList<LanguagePluginInfo>> GetLanguagePlugins()
    {
        return Ok(_languagePluginService.GetAllPlugins());
    }

    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Roles = "Admin")]
    [HttpPost("language-plugins/{code}/install")]
    public async Task<ActionResult> InstallPlugin(string code)
    {
        var result = await _languagePluginService.InstallAsync(code);
        if (!result)
            return BadRequest("Language plugin not found or is a core language");

        return Ok();
    }

    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Roles = "Admin")]
    [HttpDelete("language-plugins/{code}/uninstall")]
    public async Task<ActionResult> UninstallPlugin(string code)
    {
        var result = await _languagePluginService.UninstallAsync(code);
        if (!result)
            return BadRequest("Language plugin not found or is a core language");

        return Ok();
    }
}
