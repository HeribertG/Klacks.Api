// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Looks up existing clients or earlier import rows by their duplicate keys. A conflict is the same
/// e-mail, then the same person (name and birthdate), then the same company, postcode and street; the
/// first entry added under a key wins. Same name or same company alone are only hints.
/// </summary>
/// <typeparam name="TValue">Existing client or earlier import row</typeparam>

namespace Klacks.Api.Application.Services.ClientImport;

public class ClientImportDuplicateIndex<TValue>
    where TValue : class
{
    private readonly Dictionary<string, TValue> _byEmail = new(StringComparer.Ordinal);
    private readonly Dictionary<string, TValue> _byPerson = new(StringComparer.Ordinal);
    private readonly Dictionary<string, TValue> _byCompanyAddress = new(StringComparer.Ordinal);
    private readonly Dictionary<string, TValue> _byName = new(StringComparer.Ordinal);
    private readonly Dictionary<string, TValue> _byCompany = new(StringComparer.Ordinal);

    public void Add(ClientImportDuplicateKeys keys, TValue value)
    {
        foreach (var email in keys.Emails)
        {
            _byEmail.TryAdd(email, value);
        }

        foreach (var companyAddress in keys.CompanyAddresses)
        {
            _byCompanyAddress.TryAdd(companyAddress, value);
        }

        TryAdd(_byPerson, keys.Person, value);
        TryAdd(_byName, keys.Name, value);
        TryAdd(_byCompany, keys.Company, value);
    }

    public ClientImportDuplicateMatch<TValue>? FindConflict(ClientImportDuplicateKeys keys)
    {
        foreach (var email in keys.Emails)
        {
            if (_byEmail.TryGetValue(email, out var byEmail))
            {
                return new ClientImportDuplicateMatch<TValue>(byEmail, false);
            }
        }

        if (keys.Person != null && _byPerson.TryGetValue(keys.Person, out var byPerson))
        {
            return new ClientImportDuplicateMatch<TValue>(byPerson, false);
        }

        foreach (var companyAddress in keys.CompanyAddresses)
        {
            if (_byCompanyAddress.TryGetValue(companyAddress, out var byCompanyAddress))
            {
                return new ClientImportDuplicateMatch<TValue>(byCompanyAddress, true);
            }
        }

        return null;
    }

    public TValue? FindSameName(ClientImportDuplicateKeys keys) =>
        keys.Name != null && _byName.TryGetValue(keys.Name, out var value) ? value : null;

    public TValue? FindSameCompany(ClientImportDuplicateKeys keys) =>
        keys.Company != null && _byCompany.TryGetValue(keys.Company, out var value) ? value : null;

    private static void TryAdd(Dictionary<string, TValue> index, string? key, TValue value)
    {
        if (key != null)
        {
            index.TryAdd(key, value);
        }
    }
}
