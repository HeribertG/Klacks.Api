// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Executes one marketplace region-package update cycle: reads the installed package identity from
/// settings, asks the marketplace for the latest published version and - when a newer version passes
/// the minKlacksVersion gate and auto-update is not disabled - downloads the profile, applies the
/// re-importable entity sections via <see cref="IRegionEntityImportService"/> and records the new
/// version. All version strings are strict semantic versions: an unparsable installed, marketplace
/// or minKlacksVersion value is reported as "error" and blocks the import. Every cycle writes a
/// compact status JSON to REGION_PACKAGE_UPDATE_STATUS; a marketplace without a published package
/// for the country is reported as "packageNotFound" (a normal state, not an error), a macro
/// standard-holder conflict raised by the import as "conflict", any other failure as "error", and
/// no failure ever escapes to the caller as an unhandled exception path beyond logging. Before a
/// downloaded profile is parsed its vendor signature is checked via
/// <see cref="IRegionPackageSignatureVerifier"/>; a rejected download is reported as
/// "signatureRejected" and never applied.
/// </summary>
/// <param name="settingsRepository">Reads the package identity and writes version/status settings</param>
/// <param name="marketplaceClient">Marketplace region-package endpoints (latest version + download)</param>
/// <param name="signatureVerifier">Checks the download signature against the vendor trust root</param>
/// <param name="entityImportService">Applies the re-importable entity sections of a parsed profile</param>
/// <param name="unitOfWork">Persists version/status setting writes</param>
/// <param name="timeProvider">UTC time source for the status timestamp</param>
/// <param name="logger">Logger instance for diagnostic output</param>
using System.Text.Json;
using System.Text.Json.Serialization;
using Klacks.Api.Application.Constants;
using Klacks.Api.Application.DTOs.Config;
using Klacks.Api.Application.DTOs.Setup;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Application.Interfaces.Settings;
using Klacks.Api.Domain.Constants;
using Klacks.Api.Domain.Exceptions;
using Klacks.Api.Domain.Interfaces;
using Klacks.Api.Domain.Logging;
using Klacks.Api.Domain.Models.Update;

namespace Klacks.Api.Infrastructure.Services.Settings;

public class RegionPackageUpdateRunner : IRegionPackageUpdateRunner
{
    private const string AutoUpdateDisabledValue = "false";
    private const string ProfileSourceName = "marketplace-region-package";

    private static readonly JsonSerializerOptions StatusJsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    private readonly ISettingsRepository _settingsRepository;
    private readonly IRegionPackageMarketplaceClient _marketplaceClient;
    private readonly IRegionPackageSignatureVerifier _signatureVerifier;
    private readonly IRegionEntityImportService _entityImportService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly TimeProvider _timeProvider;
    private readonly ILogger<RegionPackageUpdateRunner> _logger;

    public RegionPackageUpdateRunner(
        ISettingsRepository settingsRepository,
        IRegionPackageMarketplaceClient marketplaceClient,
        IRegionPackageSignatureVerifier signatureVerifier,
        IRegionEntityImportService entityImportService,
        IUnitOfWork unitOfWork,
        TimeProvider timeProvider,
        ILogger<RegionPackageUpdateRunner> logger)
    {
        _settingsRepository = settingsRepository;
        _marketplaceClient = marketplaceClient;
        _signatureVerifier = signatureVerifier;
        _entityImportService = entityImportService;
        _unitOfWork = unitOfWork;
        _timeProvider = timeProvider;
        _logger = logger;
    }

    public async Task RunCycleAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var country = (await _settingsRepository.GetSettingNoTracking(SettingKeys.RegionPackageCountry))?.Value;
        var installedVersion = (await _settingsRepository.GetSettingNoTracking(SettingKeys.RegionPackageVersion))?.Value;
        if (string.IsNullOrWhiteSpace(country) || string.IsNullOrWhiteSpace(installedVersion))
        {
            _logger.LogDebug("Region package update: no marketplace package identity recorded, skipping");
            return;
        }

        var autoUpdateValue = (await _settingsRepository.GetSettingNoTracking(SettingKeys.RegionPackageAutoUpdate))?.Value;
        var autoUpdateEnabled = !string.Equals(autoUpdateValue?.Trim(), AutoUpdateDisabledValue, StringComparison.OrdinalIgnoreCase);

        var lookup = await _marketplaceClient.GetLatestAsync(country, cancellationToken);
        if (lookup.NotFound)
        {
            _logger.LogInformation(
                "Region package update: marketplace has no published package for country '{Country}'",
                country.ForLog());
            await WriteStatusAsync(installedVersion, null, RegionPackageUpdateResults.PackageNotFound, null);
            return;
        }

        var latest = lookup.Package;
        if (latest == null)
        {
            await WriteStatusAsync(
                installedVersion,
                null,
                RegionPackageUpdateResults.Error,
                $"Marketplace region package lookup for country '{country}' failed (network, server or payload error).");
            return;
        }

        if (!SemanticVersion.TryParse(installedVersion, out var installedSemanticVersion))
        {
            await WriteStatusAsync(
                installedVersion,
                latest.Version,
                RegionPackageUpdateResults.Error,
                $"Installed region package version '{installedVersion}' is not a valid semantic version (major.minor.patch).");
            return;
        }

        if (!SemanticVersion.TryParse(latest.Version, out var latestSemanticVersion))
        {
            await WriteStatusAsync(
                installedVersion,
                latest.Version,
                RegionPackageUpdateResults.Error,
                $"Marketplace version '{latest.Version}' for country '{country}' is not a valid semantic version (major.minor.patch).");
            return;
        }

        if (latestSemanticVersion <= installedSemanticVersion)
        {
            await WriteStatusAsync(installedVersion, latest.Version, RegionPackageUpdateResults.UpToDate, null);
            return;
        }

        if (!SemanticVersion.TryParse(latest.MinKlacksVersion, out var minKlacksVersion))
        {
            await WriteStatusAsync(
                installedVersion,
                latest.Version,
                RegionPackageUpdateResults.Error,
                $"Marketplace minKlacksVersion '{latest.MinKlacksVersion}' for country '{country}' is not a valid semantic version (major.minor.patch).");
            return;
        }

        var appVersion = new SemanticVersion(MyVersion.Major, MyVersion.Minor, MyVersion.Patch);
        if (minKlacksVersion > appVersion)
        {
            await WriteStatusAsync(
                installedVersion,
                latest.Version,
                RegionPackageUpdateResults.BlockedByMinVersion,
                $"Package version {latest.Version} requires Klacks {latest.MinKlacksVersion}, but this installation runs {appVersion}.");
            return;
        }

        if (!autoUpdateEnabled)
        {
            await WriteStatusAsync(installedVersion, latest.Version, RegionPackageUpdateResults.UpdateAvailableAutoOff, null);
            return;
        }

        cancellationToken.ThrowIfCancellationRequested();

        var download = await _marketplaceClient.DownloadProfileAsync(country, cancellationToken);
        if (download == null)
        {
            await WriteStatusAsync(
                installedVersion,
                latest.Version,
                RegionPackageUpdateResults.Error,
                $"Download of package version {latest.Version} for country '{country}' failed.");
            return;
        }

        var verification = _signatureVerifier.Verify(download.Content, download.Signature);
        if (!verification.Accepted)
        {
            _logger.LogWarning(
                "Region package update: signature check rejected package '{Country}' version {AvailableVersion}: {Error}",
                country.ForLog(),
                latest.Version,
                verification.Error);
            await WriteStatusAsync(installedVersion, latest.Version, RegionPackageUpdateResults.SignatureRejected, verification.Error);
            return;
        }

        RegionSetupProfile profile;
        try
        {
            profile = RegionSetupFileReader.Parse(download.ProfileJson, ProfileSourceName);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Region package update: downloaded profile for '{Country}' is not a valid region setup profile", country.ForLog());
            await WriteStatusAsync(installedVersion, latest.Version, RegionPackageUpdateResults.Error, ex.Message);
            return;
        }

        try
        {
            await _entityImportService.ApplyEntityImportsAsync(profile);
            await UpsertVersionIfChangedAsync(latest.Version);
            await WriteStatusAsync(latest.Version, latest.Version, RegionPackageUpdateResults.Updated, null);
            _logger.LogInformation(
                "Region package update: package '{Country}' updated from version {InstalledVersion} to {AvailableVersion}",
                country.ForLog(),
                installedVersion,
                latest.Version);
        }
        catch (InvalidRequestException ex)
        {
            _logger.LogWarning(ex, "Region package update: import of package '{Country}' hit a conflict", country.ForLog());
            await WriteStatusAsync(installedVersion, latest.Version, RegionPackageUpdateResults.Conflict, ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Region package update: import of package '{Country}' failed", country.ForLog());
            await WriteStatusAsync(installedVersion, latest.Version, RegionPackageUpdateResults.Error, ex.Message);
        }
    }

    private async Task UpsertVersionIfChangedAsync(string version)
    {
        var currentValue = (await _settingsRepository.GetSettingNoTracking(SettingKeys.RegionPackageVersion))?.Value;
        if (string.Equals(currentValue, version, StringComparison.Ordinal))
        {
            return;
        }

        await _settingsRepository.UpsertSettingAsync(SettingKeys.RegionPackageVersion, version);
        await _unitOfWork.CompleteAsync();
    }

    private async Task WriteStatusAsync(string installedVersion, string? availableVersion, string lastResult, string? lastError)
    {
        try
        {
            var status = new RegionPackageUpdateStatus
            {
                LastCheckUtc = _timeProvider.GetUtcNow().UtcDateTime,
                InstalledVersion = installedVersion,
                AvailableVersion = availableVersion,
                LastResult = lastResult,
                LastError = lastError
            };

            var json = JsonSerializer.Serialize(status, StatusJsonOptions);
            await _settingsRepository.UpsertSettingAsync(SettingKeys.RegionPackageUpdateStatus, json);
            await _unitOfWork.CompleteAsync();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Region package update: failed to persist the update status setting");
        }
    }
}
