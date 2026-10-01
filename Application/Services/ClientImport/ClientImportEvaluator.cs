// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// The single evaluation path of the employee import, run identically by Preview and Commit so the
/// preview can never show something other than what the commit writes: guard the request, load the
/// lookups once, transform and validate every row, then detect duplicates against the database and
/// within the file.
/// </summary>
/// <param name="lookupRepository">Contracts, groups and duplicate candidates</param>
/// <param name="countryResolver">Resolves country cells and the installation's default country</param>
/// <param name="companyClock">Company "today" for the default entry date and plausibility checks</param>
/// <param name="transformer">Row transformation</param>

using Klacks.Api.Application.DTOs.ClientImport;
using Klacks.Api.Application.Exceptions;
using Klacks.Api.Application.Interfaces.ClientImport;
using Klacks.Api.Domain.Constants;
using Klacks.Api.Domain.Enums;
using Klacks.Api.Domain.Interfaces.Settings;
using Klacks.Api.Domain.Models.Settings;

namespace Klacks.Api.Application.Services.ClientImport;

public class ClientImportEvaluator
{
    private readonly IClientImportLookupRepository _lookupRepository;
    private readonly ICountryResolver _countryResolver;
    private readonly ICompanyClock _companyClock;
    private readonly ClientImportTransformer _transformer;

    public ClientImportEvaluator(
        IClientImportLookupRepository lookupRepository,
        ICountryResolver countryResolver,
        ICompanyClock companyClock,
        ClientImportTransformer transformer)
    {
        _lookupRepository = lookupRepository;
        _countryResolver = countryResolver;
        _companyClock = companyClock;
        _transformer = transformer;
    }

    public async Task<List<ClientImportDraft>> EvaluateAsync(ClientImportRequest request, CancellationToken cancellationToken)
    {
        ClientImportRequestGuard.EnsureValid(request);

        var columns = new ClientImportColumnMap(request.Mapping);
        var lookup = await LoadLookupAsync(request, columns, cancellationToken);

        var drafts = new List<ClientImportDraft>(request.Rows.Count);
        for (var rowIndex = 0; rowIndex < request.Rows.Count; rowIndex++)
        {
            var draft = _transformer.Transform(request, columns, rowIndex, lookup);
            ClientImportValidator.Validate(draft, lookup);
            drafts.Add(draft);
        }

        lookup.ExistingClients = await _lookupRepository.FindDuplicateCandidatesAsync(
            drafts.Where(d => d.LastName != null).Select(d => d.LastName!.ToLowerInvariant()).ToList(),
            drafts.Where(d => d.Email != null).Select(d => ClientImportDuplicateDetector.EmailKey(d.Email!)).ToList(),
            cancellationToken);

        ClientImportDuplicateDetector.Detect(drafts, request.Policy.Duplicates, lookup);
        return drafts;
    }

    private async Task<ClientImportLookup> LoadLookupAsync(ClientImportRequest request, ClientImportColumnMap columns, CancellationToken cancellationToken)
    {
        var today = await _companyClock.GetTodayAsync(cancellationToken);
        var contracts = await _lookupRepository.GetContractsAsync(cancellationToken);
        var groups = await _lookupRepository.GetGroupsAsync(cancellationToken);

        return new ClientImportLookup
        {
            Today = today,
            PolicyEntryDate = ClientImportRequestGuard.ParsePolicyEntryDate(request.Policy.EntryDate) ?? today,
            DefaultCountry = await ResolveDefaultCountryAsync(request.Policy.DefaultCountry, cancellationToken),
            PolicyContract = FindPolicyEntity(request.Policy.ContractId, contracts),
            PolicyGroup = FindPolicyEntity(request.Policy.GroupId, groups),
            Contracts = contracts,
            Groups = groups,
            CountriesByValue = await ResolveCountryCellsAsync(request, columns, cancellationToken),
            RowOverrides = request.RowOverrides.ToDictionary(o => o.RowIndex)
        };
    }

    private async Task<Countries?> ResolveDefaultCountryAsync(string? policyCountry, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(policyCountry))
        {
            return await _countryResolver.GetDefaultAsync(cancellationToken);
        }

        return await _countryResolver.ResolveAsync(policyCountry, cancellationToken)
            ?? throw new ClientImportRejectedException(ClientImportErrorCodes.InvalidPolicy, $"The policy country '{policyCountry}' is unknown.");
    }

    private async Task<Dictionary<string, Countries?>> ResolveCountryCellsAsync(
        ClientImportRequest request, ClientImportColumnMap columns, CancellationToken cancellationToken)
    {
        var result = new Dictionary<string, Countries?>(StringComparer.OrdinalIgnoreCase);
        if (!columns.TryGetColumn(ClientImportTarget.Country, out var column))
        {
            return result;
        }

        var values = request.Rows
            .Select(row => column < row.Count ? row[column]?.Trim() : null)
            .Where(value => !string.IsNullOrEmpty(value))
            .Distinct(StringComparer.OrdinalIgnoreCase);

        foreach (var value in values)
        {
            result[value!] = await _countryResolver.ResolveAsync(value, cancellationToken);
        }

        return result;
    }

    private static ClientImportNamedEntity? FindPolicyEntity(Guid? id, IReadOnlyList<ClientImportNamedEntity> known)
    {
        if (!id.HasValue)
        {
            return null;
        }

        return known.FirstOrDefault(k => k.Id == id.Value)
            ?? throw new ClientImportRejectedException(ClientImportErrorCodes.InvalidPolicy, $"The policy references the unknown id {id.Value}.");
    }
}
