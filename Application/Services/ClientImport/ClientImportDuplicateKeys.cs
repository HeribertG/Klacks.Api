// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// The comparison keys of one import row or existing client for the duplicate search. Every key carries a
/// scope, so customers are compared only with customers and employees only with employees or external
/// employees. A person is keyed by e-mail, by name and birthdate (conflict) and by name alone (hint); a
/// legal entity (customer with company, its person name is only the contact) by e-mail, by company,
/// postcode and street (conflict, the
/// business key ClientRepository.FindReusableCustomerAsync uses, trimmed and lower case, all three
/// required) and by company alone (hint, a company may have several branches).
/// </summary>
/// <param name="Emails">Scoped e-mail keys</param>
/// <param name="Person">Scoped first name, last name and birthdate, or null</param>
/// <param name="Name">Scoped first and last name, or null</param>
/// <param name="Company">Scoped company of a legal entity, or null</param>
/// <param name="CompanyAddresses">Scoped company, postcode and street of a legal entity per address</param>

using Klacks.Api.Domain.Enums;

namespace Klacks.Api.Application.Services.ClientImport;

public record ClientImportDuplicateKeys(
    IReadOnlyList<string> Emails,
    string? Person,
    string? Name,
    string? Company,
    IReadOnlyList<string> CompanyAddresses)
{
    private const char KeySeparator = '|';
    private const string CustomerScope = "customer";
    private const string StaffScope = "staff";

    public static ClientImportDuplicateKeys ForDraft(ClientImportDraft draft)
    {
        var scope = Scope(draft.Type);
        var name = draft.LegalEntity ? null : NameKey(draft.FirstName, draft.LastName);
        var company = draft.LegalEntity ? BusinessKeyPart(draft.Company) : null;
        var companyAddress = CompanyAddressKey(company, draft.Zip, draft.Street);

        return new ClientImportDuplicateKeys(
            draft.Email == null ? [] : [Scoped(scope, EmailKey(draft.Email))],
            ScopedOrNull(scope, PersonKey(name, draft.Birthdate)),
            ScopedOrNull(scope, name),
            ScopedOrNull(scope, company),
            companyAddress == null ? [] : [Scoped(scope, companyAddress)]);
    }

    public static ClientImportDuplicateKeys ForExisting(ClientImportExistingClient client)
    {
        var scope = Scope(client.Type);
        var isLegalEntity = client.Type == EntityTypeEnum.Customer && client.LegalEntity;
        var name = isLegalEntity ? null : NameKey(client.FirstName, client.Name);
        var company = isLegalEntity ? BusinessKeyPart(client.Company) : null;

        return new ClientImportDuplicateKeys(
            client.Emails.Select(email => Scoped(scope, EmailKey(email))).ToList(),
            ScopedOrNull(scope, PersonKey(name, client.Birthdate)),
            ScopedOrNull(scope, name),
            ScopedOrNull(scope, company),
            client.Addresses
                .Select(address => CompanyAddressKey(company, address.Zip, address.Street))
                .Where(key => key != null)
                .Select(key => Scoped(scope, key!))
                .ToList());
    }

    public static string EmailKey(string email) => email.Trim().ToLowerInvariant();

    public static string? BusinessKeyPart(string? value)
    {
        var key = value?.Trim().ToLowerInvariant();
        return string.IsNullOrEmpty(key) ? null : key;
    }

    private static string? NameKey(string? firstName, string? lastName)
    {
        var first = ClientImportTextNormalizer.Normalize(firstName);
        var last = ClientImportTextNormalizer.Normalize(lastName);
        return last.Length == 0 ? null : string.Concat(first, KeySeparator, last);
    }

    private static string? PersonKey(string? nameKey, DateTime? birthdate) =>
        nameKey == null || !birthdate.HasValue ? null : string.Concat(nameKey, KeySeparator, ClientImportDateParser.Format(birthdate.Value));

    private static string? CompanyAddressKey(string? company, string? zip, string? street)
    {
        var zipKey = BusinessKeyPart(zip);
        var streetKey = BusinessKeyPart(street);
        return company == null || zipKey == null || streetKey == null
            ? null
            : string.Join(KeySeparator, company, zipKey, streetKey);
    }

    private static string Scope(EntityTypeEnum type) => type == EntityTypeEnum.Customer ? CustomerScope : StaffScope;

    private static string Scoped(string scope, string key) => string.Concat(scope, KeySeparator, key);

    private static string? ScopedOrNull(string scope, string? key) => key == null ? null : Scoped(scope, key);
}
