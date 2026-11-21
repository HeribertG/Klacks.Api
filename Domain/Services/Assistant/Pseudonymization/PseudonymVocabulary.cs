// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// First stage of the pseudonym dictionary: collects every name variant of every person, decides per folded phrase
/// whether it points to one person or is ambiguous, and lists the keys that need a stable number. <see cref="Bind"/>
/// turns it into the dictionary once the numbers are allocated. A phrase that names exactly one client wins over users
/// of the same name, because users and employees are often the same human; two or more clients (or, without a client,
/// two or more users) make the phrase an ambiguous NAME pseudonym. Street lines with a house number become ADR
/// pseudonyms keyed by the address id; a street that several addresses share (a household, a common workplace) takes
/// the token of the address with the lowest id, since the token restores to the street text and names nobody.
/// </summary>
/// <param name="persons">Clients (all types, deleted ones included) and users</param>
/// <param name="knownPhoneNumbers">Phone values of the communication table, matched even without a leading 0 or +</param>
/// <param name="addresses">Street lines of the client addresses (deleted ones included)</param>

using System.Globalization;
using Klacks.Api.Domain.Constants;
using Klacks.Api.Domain.Enums;

namespace Klacks.Api.Domain.Services.Assistant.Pseudonymization;

public sealed class PseudonymVocabulary
{
    private readonly List<PseudonymPerson> _persons;
    private readonly Dictionary<string, List<PseudonymNameCandidate>> _candidates = new(StringComparer.Ordinal);
    private readonly Dictionary<int, PseudonymPerson?> _idNumbers = new();
    private readonly HashSet<string> _knownPhoneTails = new(StringComparer.Ordinal);
    private readonly Dictionary<string, List<PseudonymStreetCandidate>> _streetCandidates = new(StringComparer.Ordinal);
    private readonly HashSet<string> _streetHouseNumbers = new(StringComparer.Ordinal);

    private PseudonymVocabulary(List<PseudonymPerson> persons)
    {
        _persons = persons;
    }

    public static PseudonymVocabulary Create(
        IEnumerable<PseudonymPerson> persons,
        IEnumerable<string> knownPhoneNumbers,
        IEnumerable<PseudonymAddress> addresses)
    {
        var vocabulary = new PseudonymVocabulary(persons.ToList());
        foreach (var person in vocabulary._persons)
        {
            vocabulary.AddPerson(person);
        }

        foreach (var phone in knownPhoneNumbers)
        {
            var tail = PseudonymPhoneNumbers.ComparisonTail(phone);
            if (tail.Length >= PseudonymizationConstants.MinimumKnownPhoneDigits)
            {
                vocabulary._knownPhoneTails.Add(tail);
            }
        }

        foreach (var address in addresses)
        {
            vocabulary.AddAddress(address);
        }

        return vocabulary;
    }

    public IReadOnlyCollection<PseudonymKey> RequiredKeys
    {
        get
        {
            var keys = new HashSet<PseudonymKey>(_persons.Select(person => new PseudonymKey(person.Kind, person.EntityKey)));
            foreach (var candidates in _candidates.Values)
            {
                if (Resolve(candidates) == null)
                {
                    keys.Add(new PseudonymKey(PseudonymKind.Name, AmbiguousKey(AmbiguousRepresentative(candidates))));
                }
            }

            foreach (var candidates in _streetCandidates.Values)
            {
                keys.Add(new PseudonymKey(PseudonymKind.Address, StreetRepresentative(candidates).EntityKey));
            }

            return keys;
        }
    }

    public PseudonymDictionary Bind(IReadOnlyDictionary<PseudonymKey, int> ordinals)
    {
        var phrases = new Dictionary<string, PseudonymPhrase>(StringComparer.Ordinal);
        var reverse = new Dictionary<string, string>(StringComparer.Ordinal);
        var legend = new Dictionary<string, string>(StringComparer.Ordinal);
        var idNumberTokens = new Dictionary<int, string>();

        foreach (var person in _persons)
        {
            var baseToken = BaseToken(person, ordinals);
            reverse[baseToken.ToString()] = DisplayName(person);
            legend[baseToken.ToString()] = LegendText(person);
            foreach (var (variant, surface) in Variants(person))
            {
                reverse[(baseToken with { Variant = variant }).ToString()] = surface;
            }

            if (person.IdNumber is { } idNumber && _idNumbers.TryGetValue(idNumber, out var owner) && owner == person)
            {
                var token = (baseToken with { Variant = PseudonymizationConstants.PersonnelNumberVariant }).ToString();
                idNumberTokens[idNumber] = token;
                reverse[token] = idNumber.ToString(CultureInfo.InvariantCulture);
            }
        }

        foreach (var (key, candidates) in _candidates)
        {
            var chosen = Resolve(candidates);
            if (chosen != null)
            {
                var baseToken = BaseToken(chosen.Person, ordinals);
                var token = baseToken with { Variant = chosen.Variant };
                phrases[key] = new PseudonymPhrase(token.ToString(), baseToken.ToString(), chosen.Surface);
                continue;
            }

            var representative = AmbiguousRepresentative(candidates);
            var nameToken = new PseudonymToken(
                PseudonymKind.Name, ordinals[new PseudonymKey(PseudonymKind.Name, AmbiguousKey(representative))]).ToString();
            var nameSurface = representative.Surface;
            phrases[key] = new PseudonymPhrase(nameToken, nameToken, nameSurface);
            reverse[nameToken] = nameSurface;
        }

        var streets = new Dictionary<string, PseudonymPhrase>(StringComparer.Ordinal);
        foreach (var (key, candidates) in _streetCandidates)
        {
            var representative = StreetRepresentative(candidates);
            var baseToken = new PseudonymToken(
                PseudonymKind.Address, ordinals[new PseudonymKey(PseudonymKind.Address, representative.EntityKey)]);
            var token = (baseToken with { Variant = representative.Variant }).ToString();
            streets[key] = new PseudonymPhrase(token, baseToken.ToString(), representative.Surface);
            reverse[token] = representative.Surface;
        }

        return new PseudonymDictionary(
            phrases, reverse, legend, idNumberTokens, _knownPhoneTails, streets, _streetHouseNumbers);
    }

    private static PseudonymToken BaseToken(PseudonymPerson person, IReadOnlyDictionary<PseudonymKey, int> ordinals) =>
        new(person.Kind, ordinals[new PseudonymKey(person.Kind, person.EntityKey)]);

    private void AddPerson(PseudonymPerson person)
    {
        foreach (var (variant, surface) in Variants(person))
        {
            var keys = PseudonymWords.PhraseKeys(
                surface, PseudonymizationConstants.MaxPhraseWords, PseudonymizationConstants.MinimumNameLetters);
            if (keys.Count == 0)
            {
                continue;
            }

            var candidate = new PseudonymNameCandidate(person, variant, surface, keys[0]);
            foreach (var key in keys)
            {
                if (!_candidates.TryGetValue(key, out var list))
                {
                    list = new List<PseudonymNameCandidate>();
                    _candidates[key] = list;
                }

                if (!list.Any(existing => ReferenceEquals(existing.Person, person)))
                {
                    list.Add(candidate);
                }
            }
        }

        if (person.IdNumber is { } idNumber && idNumber > 0)
        {
            _idNumbers[idNumber] = _idNumbers.ContainsKey(idNumber) ? null : person;
        }
    }

    private void AddAddress(PseudonymAddress address)
    {
        foreach (var (variant, line) in AddressLines(address))
        {
            var parsed = PseudonymStreets.Parse(line);
            if (parsed == null)
            {
                continue;
            }

            var candidate = new PseudonymStreetCandidate(address.EntityKey, variant, parsed.Surface);
            _streetHouseNumbers.Add(parsed.HouseNumber);
            foreach (var key in parsed.Keys)
            {
                if (!_streetCandidates.TryGetValue(key, out var list))
                {
                    list = new List<PseudonymStreetCandidate>();
                    _streetCandidates[key] = list;
                }

                list.Add(candidate);
            }
        }
    }

    private static IEnumerable<(string? Variant, string? Line)> AddressLines(PseudonymAddress address)
    {
        yield return (null, address.Street);
        yield return (PseudonymizationConstants.Street2Variant, address.Street2);
        yield return (PseudonymizationConstants.Street3Variant, address.Street3);
        yield return (PseudonymizationConstants.AddressLine1Variant, address.AddressLine1);
        yield return (PseudonymizationConstants.AddressLine2Variant, address.AddressLine2);
    }

    private static PseudonymStreetCandidate StreetRepresentative(List<PseudonymStreetCandidate> candidates) =>
        candidates
            .OrderBy(candidate => candidate.EntityKey, StringComparer.Ordinal)
            .ThenBy(candidate => candidate.Variant ?? string.Empty, StringComparer.Ordinal)
            .First();

    private static PseudonymNameCandidate? Resolve(List<PseudonymNameCandidate> candidates)
    {
        var clients = candidates.Where(candidate => candidate.Person.Kind != PseudonymKind.User).ToList();
        if (clients.Count > 0)
        {
            return clients.Count == 1 ? clients[0] : null;
        }

        return candidates.Count == 1 ? candidates[0] : null;
    }

    /// <summary>
    /// The candidate that stands for an ambiguous phrase: lowest folded spelling, then lowest entity key, so both fold
    /// modes of one name meet on the same candidate.
    /// </summary>
    private static PseudonymNameCandidate AmbiguousRepresentative(List<PseudonymNameCandidate> candidates) =>
        candidates
            .OrderBy(candidate => candidate.PrimaryKey, StringComparer.Ordinal)
            .ThenBy(candidate => candidate.Person.Kind)
            .ThenBy(candidate => candidate.Person.EntityKey, StringComparer.Ordinal)
            .ThenBy(candidate => candidate.Variant, StringComparer.Ordinal)
            .First();

    /// <summary>
    /// The stored key of an ambiguous name: kind, entity id and variant of its representative, never the name itself.
    /// </summary>
    private static string AmbiguousKey(PseudonymNameCandidate representative) =>
        string.Join(
            PseudonymizationConstants.TokenSeparator,
            (int)representative.Person.Kind,
            representative.Person.EntityKey,
            representative.Variant ?? string.Empty);

    private static string DisplayName(PseudonymPerson person)
    {
        if (person.LegalEntity && !string.IsNullOrWhiteSpace(person.Company))
        {
            return person.Company.Trim();
        }

        var full = string.Join(' ', new[] { person.FirstName, person.LastName }
            .Where(part => !string.IsNullOrWhiteSpace(part))
            .Select(part => part!.Trim()));
        return full.Length > 0 ? full : person.Company?.Trim() ?? string.Empty;
    }

    private static string LegendText(PseudonymPerson person)
    {
        if (person.LegalEntity || person.Gender == GenderEnum.LegalEntity)
        {
            return PseudonymizationConstants.LegendOrganisation;
        }

        return person.Gender switch
        {
            GenderEnum.Female => PseudonymizationConstants.LegendFemale,
            GenderEnum.Male => PseudonymizationConstants.LegendMale,
            _ => PseudonymizationConstants.LegendOther
        };
    }

    /// <summary>
    /// Every way the person's name may appear, longest first. The full name carries no suffix.
    /// </summary>
    internal static IEnumerable<(string? Variant, string Surface)> Variants(PseudonymPerson person)
    {
        var first = person.FirstName?.Trim();
        var last = person.LastName?.Trim();
        var second = person.SecondName?.Trim();
        var hasFirst = !string.IsNullOrEmpty(first);
        var hasLast = !string.IsNullOrEmpty(last);

        if (hasFirst && hasLast && !string.IsNullOrEmpty(second))
        {
            yield return (PseudonymizationConstants.MiddleNameFullVariant, $"{first} {second} {last}");
        }

        if (hasFirst && hasLast)
        {
            yield return (null, $"{first} {last}");
            yield return (PseudonymizationConstants.LastFirstVariant, $"{last} {first}");
            yield return (PseudonymizationConstants.LastCommaFirstVariant, $"{last}, {first}");
        }

        if (hasLast)
        {
            yield return (PseudonymizationConstants.LastNameVariant, last!);
            foreach (var part in DoubleNameParts(last!))
            {
                yield return part;
            }
        }

        if (hasFirst)
        {
            yield return (PseudonymizationConstants.FirstNameVariant, first!);
            var firstParts = first!.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (firstParts.Length > 1)
            {
                yield return (PseudonymizationConstants.FirstNamePartVariant, firstParts[0]);
            }
        }

        if (!string.IsNullOrWhiteSpace(person.MaidenName))
        {
            yield return (PseudonymizationConstants.MaidenNameVariant, person.MaidenName.Trim());
        }

        if (person.LegalEntity && !string.IsNullOrWhiteSpace(person.Company))
        {
            yield return (PseudonymizationConstants.CompanyVariant, person.Company.Trim());
        }
    }

    private static IEnumerable<(string? Variant, string Surface)> DoubleNameParts(string lastName)
    {
        if (lastName.Contains(' '))
        {
            yield break;
        }

        var parts = lastName.Split(
            PseudonymizationConstants.TokenSeparator, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (parts.Length <= 1)
        {
            yield break;
        }

        for (var i = 0; i < parts.Length; i++)
        {
            var number = (i + 1).ToString(CultureInfo.InvariantCulture);
            yield return (PseudonymizationConstants.DoubleNamePartVariantPrefix + number, parts[i]);
        }
    }
}
