// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Resolves the start or end base stored on a container template to the address the route is optimized from, like
/// the Ui container page does: the stored value is matched against the company address (name = APP_ADDRESS_NAME or
/// else APP_NAME, address = street plus "zip place") and the branches, first by name and then by address. Unlike the
/// Ui, a stored value that matches no entry is used as it is (a container built by
/// bundle_nearby_timerange_shifts_into_container stores the city it was bundled around), so it is geocoded directly
/// instead of dropping the base.
/// </summary>
/// <param name="settingsReader">Reads the company address settings</param>
/// <param name="branchRepository">Lists the branches</param>

using AppSettingKeys = Klacks.Api.Application.Constants.Settings;
using Klacks.Api.Application.Interfaces.Schedules;
using Klacks.Api.Domain.Interfaces.Settings;

namespace Klacks.Api.Application.Services.Schedules;

public class ContainerBaseAddressResolver : IContainerBaseAddressResolver
{
    private const string AddressPartSeparator = ", ";
    private const string ZipPlaceSeparator = " ";

    private static readonly string[] CompanyAddressSettings =
    [
        AppSettingKeys.APP_NAME,
        AppSettingKeys.APP_ADDRESS_NAME,
        AppSettingKeys.APP_ADDRESS_ADDRESS,
        AppSettingKeys.APP_ADDRESS_ZIP,
        AppSettingKeys.APP_ADDRESS_PLACE
    ];

    private readonly ISettingsReader _settingsReader;
    private readonly IBranchRepository _branchRepository;

    public ContainerBaseAddressResolver(ISettingsReader settingsReader, IBranchRepository branchRepository)
    {
        _settingsReader = settingsReader;
        _branchRepository = branchRepository;
    }

    public async Task<string> ResolveAsync(string? storedBase, CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(storedBase))
        {
            return string.Empty;
        }

        var entries = await LoadEntriesAsync(cancellationToken);
        var match = entries.FirstOrDefault(e => e.Name == storedBase)
            ?? entries.FirstOrDefault(e => e.Address == storedBase);

        return match is null ? storedBase : match.Address;
    }

    private sealed record BaseEntry(string Name, string Address);

    private async Task<List<BaseEntry>> LoadEntriesAsync(CancellationToken cancellationToken)
    {
        var settings = await _settingsReader.GetSettingsByTypesAsync(CompanyAddressSettings, cancellationToken);
        string Value(string key) => settings.TryGetValue(key, out var value) ? value ?? string.Empty : string.Empty;

        var entries = new List<BaseEntry>();
        var companyName = FirstNonEmpty(Value(AppSettingKeys.APP_ADDRESS_NAME), Value(AppSettingKeys.APP_NAME));
        var companyAddress = FormatAddress(
            Value(AppSettingKeys.APP_ADDRESS_ADDRESS), Value(AppSettingKeys.APP_ADDRESS_ZIP), Value(AppSettingKeys.APP_ADDRESS_PLACE));
        if (companyName.Length > 0 || companyAddress.Length > 0)
        {
            entries.Add(new BaseEntry(companyName, companyAddress));
        }

        var branches = await _branchRepository.List();
        entries.AddRange(branches
            .Where(b => !string.IsNullOrEmpty(b.Name) || !string.IsNullOrEmpty(b.Address))
            .Select(b => new BaseEntry(b.Name, b.Address)));

        return entries;
    }

    private static string FirstNonEmpty(string first, string second) => first.Length > 0 ? first : second;

    private static string FormatAddress(string street, string zip, string place)
    {
        var parts = new List<string>();
        if (street.Length > 0)
        {
            parts.Add(street);
        }

        var zipPlace = string.Join(ZipPlaceSeparator, new[] { zip, place }.Where(p => p.Length > 0)).Trim();
        if (zipPlace.Length > 0)
        {
            parts.Add(zipPlace);
        }

        return string.Join(AddressPartSeparator, parts);
    }
}
