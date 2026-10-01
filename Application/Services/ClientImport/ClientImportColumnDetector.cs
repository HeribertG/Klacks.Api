// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Proposes a target for every column of an import file. A header that equals a known synonym of any
/// supported language wins with full confidence; a header that contains one is a weaker hint; a column
/// without a usable header is classified by its content (e-mails, gender words, "postcode city", dates,
/// phone numbers, postcodes). Every target except Ignore is given to at most one column (the most
/// confident); the others fall back to Ignore. A header that holds both a first-name and a last-name word
/// ("Nachname, Vorname", "Surname and given name") is one full-name column. A generic "name" column next
/// to a separate first-name column is read as the last name. The name order of a full-name column comes
/// from its values when most of them hold a comma ("Mueller, Anna"), otherwise from the order of the name
/// words in its header ("Name, Vorname", "Cognome e nome" are last-first), otherwise first-last.
/// </summary>
/// <param name="catalog">Header and gender vocabulary of all supported languages</param>

using System.Globalization;
using Klacks.Api.Application.DTOs.ClientImport;
using Klacks.Api.Domain.Constants;
using Klacks.Api.Domain.Enums;

namespace Klacks.Api.Application.Services.ClientImport;

public class ClientImportColumnDetector
{
    public const double ExactHeaderConfidence = 1.0;
    public const double CombinedNameHeaderConfidence = 0.9;
    public const double ContainedHeaderConfidence = 0.7;
    public const double EmailContentConfidence = 0.9;
    public const double GenderContentConfidence = 0.7;
    public const double ZipCityContentConfidence = 0.7;
    public const double PhoneContentConfidence = 0.6;
    public const double DateContentConfidence = 0.5;
    public const double ZipContentConfidence = 0.5;

    private const int MinContainedSynonymLength = 4;
    private const double ContentMatchShare = 0.8;
    private const int AdultAgeYears = 16;
    private const int FourDigitYearLength = 4;
    private const int AnyNameWordLength = 1;
    private const char FullNameComma = ',';

    private readonly ClientImportSynonymCatalog _catalog;

    public ClientImportColumnDetector(ClientImportSynonymCatalog catalog)
    {
        _catalog = catalog;
    }

    /// <param name="headers">Header text per column</param>
    /// <param name="rows">Data rows (without header) used for content detection</param>
    /// <param name="currentYear">Company's current year, separates birth dates from entry dates</param>
    public List<ClientImportColumnMapping> Detect(IReadOnlyList<string> headers, IReadOnlyList<IReadOnlyList<string>> rows, int currentYear)
    {
        var candidates = new List<ClientImportColumnMapping>();

        for (var column = 0; column < headers.Count; column++)
        {
            candidates.Add(DetectColumn(column, headers[column], ColumnValues(rows, column), currentYear));
        }

        var winners = candidates
            .Where(c => c.Target != ClientImportTarget.Ignore)
            .GroupBy(c => c.Target)
            .Select(group => group.OrderByDescending(c => c.Confidence).ThenBy(c => c.ColumnIndex).First())
            .ToHashSet();

        var result = candidates
            .Select(c => winners.Contains(c) ? c : new ClientImportColumnMapping { ColumnIndex = c.ColumnIndex, Target = ClientImportTarget.Ignore, Confidence = 0 })
            .ToList();

        ReadGenericNameAsLastName(result, headers);
        return result;
    }

    /// <param name="headers">Header text per column</param>
    /// <param name="mapping">Detected or user-chosen mapping</param>
    /// <param name="rows">Data rows (without header)</param>
    public ClientImportNameOrder DetectNameOrder(IReadOnlyList<string> headers, IReadOnlyList<ClientImportColumnMapping> mapping, IReadOnlyList<IReadOnlyList<string>> rows)
    {
        var fullName = mapping.FirstOrDefault(m => m.Target == ClientImportTarget.FullName);
        if (fullName == null)
        {
            return ClientImportNameOrder.FirstLast;
        }

        var values = ColumnValues(rows, fullName.ColumnIndex);
        if (values.Count > 0 && Share(values, v => v.Contains(FullNameComma)) >= ContentMatchShare)
        {
            return ClientImportNameOrder.LastFirst;
        }

        var header = fullName.ColumnIndex < headers.Count ? headers[fullName.ColumnIndex] : null;
        return HeaderNameOrder(header) ?? ClientImportNameOrder.FirstLast;
    }

    private ClientImportColumnMapping DetectColumn(int column, string header, List<string> values, int currentYear)
    {
        var exact = _catalog.MatchHeader(header);
        if (exact.HasValue)
        {
            return Mapping(column, exact.Value, ExactHeaderConfidence);
        }

        if (IsCombinedNameHeader(header))
        {
            return Mapping(column, ClientImportTarget.FullName, CombinedNameHeaderConfidence);
        }

        var contained = _catalog.MatchHeaderContaining(header, MinContainedSynonymLength);
        if (contained.HasValue)
        {
            return Mapping(column, contained.Value, ContainedHeaderConfidence);
        }

        return DetectByContent(column, values, currentYear);
    }

    private ClientImportColumnMapping DetectByContent(int column, List<string> values, int currentYear)
    {
        if (values.Count == 0)
        {
            return Mapping(column, ClientImportTarget.Ignore, 0);
        }

        if (Share(values, ClientImportValueClassifier.IsEmail) >= ContentMatchShare)
        {
            return Mapping(column, ClientImportTarget.Email, EmailContentConfidence);
        }

        if (Share(values, v => _catalog.ResolveGender(v).HasValue) >= ContentMatchShare)
        {
            return Mapping(column, ClientImportTarget.Gender, GenderContentConfidence);
        }

        if (Share(values, ClientImportValueClassifier.IsDate) >= ContentMatchShare)
        {
            var target = LooksLikeBirthdates(values, currentYear) ? ClientImportTarget.Birthdate : ClientImportTarget.EntryDate;
            return Mapping(column, target, DateContentConfidence);
        }

        if (Share(values, ClientImportValueClassifier.IsZipCity) >= ContentMatchShare)
        {
            return Mapping(column, ClientImportTarget.ZipCity, ZipCityContentConfidence);
        }

        if (Share(values, ClientImportValueClassifier.IsPhone) >= ContentMatchShare)
        {
            return Mapping(column, ClientImportTarget.Phone, PhoneContentConfidence);
        }

        if (Share(values, ClientImportValueClassifier.IsZip) >= ContentMatchShare)
        {
            return Mapping(column, ClientImportTarget.Zip, ZipContentConfidence);
        }

        return Mapping(column, ClientImportTarget.Ignore, 0);
    }

    private static bool LooksLikeBirthdates(List<string> values, int currentYear)
    {
        var years = new List<int>();
        foreach (var value in values)
        {
            if (!ClientImportDateParser.TrySplit(value, out var first, out _, out var third))
            {
                continue;
            }

            var yearText = first.Length == FourDigitYearLength ? first : third;
            var year = int.Parse(yearText, NumberStyles.None, CultureInfo.InvariantCulture);
            years.Add(yearText.Length == FourDigitYearLength ? year : ClientImportDateParser.ExpandTwoDigitYear(year, currentYear));
        }

        if (years.Count == 0)
        {
            return false;
        }

        years.Sort();
        return currentYear - years[years.Count / 2] >= AdultAgeYears;
    }

    private bool IsCombinedNameHeader(string header)
    {
        var parts = _catalog.FindNameParts(header, MinContainedSynonymLength);
        return parts.FirstNamePosition.HasValue && parts.LastNamePosition.HasValue;
    }

    private ClientImportNameOrder? HeaderNameOrder(string? header)
    {
        var parts = _catalog.FindNameParts(header, AnyNameWordLength);
        if (!parts.FirstNamePosition.HasValue)
        {
            return null;
        }

        var lastNamePosition = parts.LastNamePosition ?? parts.GenericNamePosition;
        if (!lastNamePosition.HasValue)
        {
            return null;
        }

        return lastNamePosition.Value < parts.FirstNamePosition.Value ? ClientImportNameOrder.LastFirst : ClientImportNameOrder.FirstLast;
    }

    private void ReadGenericNameAsLastName(List<ClientImportColumnMapping> mappings, IReadOnlyList<string> headers)
    {
        var hasFirstName = mappings.Any(m => m.Target == ClientImportTarget.FirstName);
        var hasLastName = mappings.Any(m => m.Target == ClientImportTarget.LastName);
        var fullName = mappings.FirstOrDefault(m => m.Target == ClientImportTarget.FullName);

        if (hasFirstName && !hasLastName && fullName != null && !HoldsAFirstNameWord(headers[fullName.ColumnIndex]))
        {
            fullName.Target = ClientImportTarget.LastName;
        }
    }

    private bool HoldsAFirstNameWord(string header) =>
        _catalog.FindNameParts(header, AnyNameWordLength).FirstNamePosition.HasValue;

    private static List<string> ColumnValues(IReadOnlyList<IReadOnlyList<string>> rows, int column) =>
        rows.Take(ClientImportLimits.ContentSampleRows)
            .Select(row => column < row.Count ? row[column] : string.Empty)
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .ToList();

    private static double Share(List<string> values, Func<string, bool> predicate) =>
        (double)values.Count(predicate) / values.Count;

    private static ClientImportColumnMapping Mapping(int column, ClientImportTarget target, double confidence) =>
        new() { ColumnIndex = column, Target = target, Confidence = confidence };
}
