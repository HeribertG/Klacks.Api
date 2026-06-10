// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Resolves the active language configuration: supported languages and fallback order from app
/// configuration (with core defaults as fallback), merged with installed language plugin codes
/// and their metadata.
/// </summary>
/// <param name="configuration">App configuration providing the Languages section.</param>
/// <param name="languagePluginService">Language plugin discovery and installation state.</param>

using Klacks.Api.Application.DTOs.Config;
using Klacks.Api.Application.Interfaces.Settings;
using Klacks.Api.Application.Queries.Settings.Languages;
using Klacks.Api.Domain.Common;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Handlers.Settings.Languages;

public class GetLanguagesQueryHandler : IRequestHandler<GetLanguagesQuery, LanguageConfigResponse>
{
    private const string LanguagesSectionKey = "Languages";
    private const string SupportedSectionKey = "Supported";
    private const string FallbackOrderSectionKey = "FallbackOrder";
    private const string MetadataSectionKey = "Metadata";

    private readonly IConfiguration _configuration;
    private readonly ILanguagePluginService _languagePluginService;

    public GetLanguagesQueryHandler(
        IConfiguration configuration,
        ILanguagePluginService languagePluginService)
    {
        _configuration = configuration;
        _languagePluginService = languagePluginService;
    }

    public Task<LanguageConfigResponse> Handle(GetLanguagesQuery request, CancellationToken cancellationToken)
    {
        var languagesSection = _configuration.GetSection(LanguagesSectionKey);

        var supported = languagesSection.GetSection(SupportedSectionKey).Get<string[]>();
        if (supported == null || supported.Length == 0)
        {
            supported = LanguageConfig.SupportedLanguages;
        }

        var fallbackOrder = languagesSection.GetSection(FallbackOrderSectionKey).Get<string[]>();
        if (fallbackOrder == null || fallbackOrder.Length == 0)
        {
            fallbackOrder = LanguageConfig.FallbackOrder;
        }

        var metadata = languagesSection.GetSection(MetadataSectionKey).Get<Dictionary<string, LanguageMetadata>>()
            ?? new Dictionary<string, LanguageMetadata>();

        var allSupported = supported.ToList();

        foreach (var code in _languagePluginService.GetInstalledPluginCodes())
        {
            if (allSupported.Contains(code))
            {
                continue;
            }

            allSupported.Add(code);

            var plugin = _languagePluginService.GetPlugin(code);
            if (plugin != null && !metadata.ContainsKey(code))
            {
                metadata[code] = new LanguageMetadata
                {
                    Name = plugin.Name,
                    DisplayName = plugin.DisplayName,
                    SpeechLocale = plugin.SpeechLocale,
                    Direction = plugin.Direction
                };
            }
        }

        return Task.FromResult(new LanguageConfigResponse
        {
            SupportedLanguages = allSupported.ToArray(),
            FallbackOrder = fallbackOrder,
            Metadata = metadata
        });
    }
}
