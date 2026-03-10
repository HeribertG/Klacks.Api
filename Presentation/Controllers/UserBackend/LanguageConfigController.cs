// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using System.Text.Json;
using Klacks.Api.Domain.Common;
using Klacks.Api.Domain.Constants;
using Klacks.Api.Application.DTOs.Config;
using Klacks.Api.Application.Interfaces.Settings;
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
    private readonly IMarketplaceClientService _marketplaceClient;

    public LanguageConfigController(
        IConfiguration configuration,
        ILanguagePluginService languagePluginService,
        IMarketplaceClientService marketplaceClient)
    {
        _configuration = configuration;
        _languagePluginService = languagePluginService;
        _marketplaceClient = marketplaceClient;
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

    [HttpGet("language-plugins/{code}/docs/{manualName}")]
    public async Task<ActionResult> GetPluginDoc(string code, string manualName)
    {
        var html = await _languagePluginService.GetPluginDocAsync(code, manualName);
        if (html == null)
            return NotFound();

        return Content(html, "text/html");
    }

    [HttpGet("language-plugins")]
    public ActionResult<IReadOnlyList<LanguagePluginInfo>> GetLanguagePlugins()
    {
        return Ok(_languagePluginService.GetAllPlugins());
    }

    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Roles = Roles.Admin)]
    [HttpPost("language-plugins/{code}/install")]
    public async Task<ActionResult> InstallPlugin(string code)
    {
        var result = await _languagePluginService.InstallAsync(code);
        if (!result)
            return BadRequest("Language plugin not found or is a core language");

        return Ok();
    }

    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Roles = Roles.Admin)]
    [HttpDelete("language-plugins/{code}/uninstall")]
    public async Task<ActionResult> UninstallPlugin(string code)
    {
        var result = await _languagePluginService.UninstallAsync(code);
        if (!result)
            return BadRequest("Language plugin not found or is a core language");

        return Ok();
    }

    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
    [HttpGet("marketplace/packages")]
    public async Task<ActionResult<MarketplaceSearchResultDto>> SearchMarketplacePackages(
        [FromQuery] string? search,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        var result = await _marketplaceClient.SearchPackagesAsync(search, page, pageSize);
        if (result == null)
            return StatusCode(503, "Marketplace service unavailable");

        return Ok(result);
    }

    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
    [HttpGet("marketplace/packages/{code}")]
    public async Task<ActionResult<MarketplacePackageDto>> GetMarketplacePackage(string code)
    {
        var result = await _marketplaceClient.GetPackageAsync(code);
        if (result == null)
            return NotFound();

        return Ok(result);
    }

    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Roles = Roles.Admin)]
    [HttpPost("marketplace/packages/{code}/download-and-install")]
    public async Task<ActionResult> DownloadAndInstallPackage(string code)
    {
        var bundle = await _marketplaceClient.DownloadPackageAsync(code);
        if (bundle == null)
            return StatusCode(503, "Marketplace service unavailable");

        var pluginDir = Path.Combine(AppContext.BaseDirectory,
            _configuration.GetValue<string>("LanguagePlugins:Directory") ?? "Plugins/Languages");
        var packageDir = Path.Combine(pluginDir, code);
        Directory.CreateDirectory(packageDir);

        using var jsonDoc = JsonDocument.Parse(bundle);
        var root = jsonDoc.RootElement;

        var jsonWriteOptions = new JsonWriterOptions { Indented = true };

        if (root.TryGetProperty("manifest", out var manifest))
            await WriteJsonFileAsync(Path.Combine(packageDir, "manifest.json"), manifest, jsonWriteOptions);

        if (root.TryGetProperty("translations", out var translations))
            await WriteJsonFileAsync(Path.Combine(packageDir, "translations.json"), translations, jsonWriteOptions);

        if (root.TryGetProperty("countries", out var countries))
            await WriteJsonFileAsync(Path.Combine(packageDir, "countries.json"), countries, jsonWriteOptions);

        if (root.TryGetProperty("states", out var states))
            await WriteJsonFileAsync(Path.Combine(packageDir, "states.json"), states, jsonWriteOptions);

        if (root.TryGetProperty("calendarRules", out var calendarRules))
            await WriteJsonFileAsync(Path.Combine(packageDir, "calendar-rules.json"), calendarRules, jsonWriteOptions);

        if (root.TryGetProperty("docs", out var docs))
        {
            var docsDir = Path.Combine(packageDir, "docs");
            Directory.CreateDirectory(docsDir);

            foreach (var docProperty in docs.EnumerateObject())
            {
                var filePath = Path.Combine(docsDir, $"{docProperty.Name}.html");
                await System.IO.File.WriteAllTextAsync(filePath, docProperty.Value.GetString() ?? string.Empty);
            }
        }

        _languagePluginService.RefreshPlugins();
        await _languagePluginService.InstallAsync(code);

        return Ok();
    }

    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Roles = Roles.Admin)]
    [HttpPost("marketplace/packages/{code}/upload")]
    public async Task<ActionResult> UploadPackageToMarketplace(string code)
    {
        var pluginDir = Path.Combine(AppContext.BaseDirectory,
            _configuration.GetValue<string>("LanguagePlugins:Directory") ?? "Plugins/Languages");
        var packageDir = Path.Combine(pluginDir, code);

        if (!Directory.Exists(packageDir))
            return NotFound($"Plugin directory for '{code}' not found");

        var manifestPath = Path.Combine(packageDir, "manifest.json");
        if (!System.IO.File.Exists(manifestPath))
            return BadRequest("manifest.json not found in plugin directory");

        var translationsPath = Path.Combine(packageDir, "translations.json");
        if (!System.IO.File.Exists(translationsPath))
            return BadRequest("translations.json not found in plugin directory");

        var manifestJson = await System.IO.File.ReadAllTextAsync(manifestPath);
        var translationsJson = await System.IO.File.ReadAllTextAsync(translationsPath);

        var countriesPath = Path.Combine(packageDir, "countries.json");
        string? countriesJson = System.IO.File.Exists(countriesPath)
            ? await System.IO.File.ReadAllTextAsync(countriesPath)
            : null;

        var statesPath = Path.Combine(packageDir, "states.json");
        string? statesJson = System.IO.File.Exists(statesPath)
            ? await System.IO.File.ReadAllTextAsync(statesPath)
            : null;

        var calendarRulesPath = Path.Combine(packageDir, "calendar-rules.json");
        string? calendarRulesJson = System.IO.File.Exists(calendarRulesPath)
            ? await System.IO.File.ReadAllTextAsync(calendarRulesPath)
            : null;

        string? docsJson = null;
        var docsDir = Path.Combine(packageDir, "docs");
        if (Directory.Exists(docsDir))
        {
            var htmlFiles = Directory.GetFiles(docsDir, "*.html");
            if (htmlFiles.Length > 0)
            {
                var docsDict = new Dictionary<string, string>();
                foreach (var htmlFile in htmlFiles)
                {
                    var docName = Path.GetFileNameWithoutExtension(htmlFile);
                    var content = await System.IO.File.ReadAllTextAsync(htmlFile);
                    docsDict[docName] = content;
                }
                docsJson = JsonSerializer.Serialize(docsDict);
            }
        }

        var result = await _marketplaceClient.UploadPackageAsync(
            manifestJson, translationsJson, docsJson, countriesJson, statesJson, calendarRulesJson);

        if (!result)
            return StatusCode(503, "Failed to upload package to marketplace");

        return Ok();
    }

    private static async Task WriteJsonFileAsync(string path, JsonElement element, JsonWriterOptions options)
    {
        using var stream = System.IO.File.Create(path);
        using var writer = new Utf8JsonWriter(stream, options);
        element.WriteTo(writer);
    }
}
