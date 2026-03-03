// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Application.DTOs.Config;

namespace Klacks.Api.Domain.Interfaces.Settings;

public interface ILanguagePluginService
{
    IReadOnlyList<LanguagePluginInfo> GetAllPlugins();
    LanguagePluginInfo? GetPlugin(string code);
    Task<bool> InstallAsync(string code);
    Task<bool> UninstallAsync(string code);
    Dictionary<string, string>? GetTranslations(string code);
    IReadOnlyList<string> GetInstalledPluginCodes();
    void RefreshPlugins();
}
