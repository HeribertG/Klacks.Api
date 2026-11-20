// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Limits, token parts and model-facing texts of the reversible pseudonymisation that runs in front of every cloud
/// language model. Kept in one place so the forward pass, the return path and the stream holdback agree on the
/// token shape.
/// </summary>

namespace Klacks.Api.Domain.Constants;

public static class PseudonymizationConstants
{
    public const int TokenDigits = 4;

    public const int MinimumNameLetters = 3;

    public const int MaxPhraseWords = 4;

    public const int MinimumUnknownPhoneDigits = 9;

    public const int MinimumKnownPhoneDigits = 7;

    public const int PhoneComparisonDigits = 9;

    public const int MaxStreamHoldbackChars = 24;

    public const char TokenSeparator = '-';

    public const string FirstNameVariant = "V";

    public const string FirstNamePartVariant = "V1";

    public const string LastNameVariant = "N";

    public const string MaidenNameVariant = "L";

    public const string LastFirstVariant = "S";

    public const string LastCommaFirstVariant = "R";

    public const string MiddleNameFullVariant = "M";

    public const string CompanyVariant = "C";

    public const string PersonnelNumberVariant = "P";

    public const string DoubleNamePartVariantPrefix = "D";

    public const string BirthdateAgeFormat = "age {0}";

    /// <summary>
    /// Prefix that replaces an invented pseudonym in tool arguments. It starts with a NUL character so no real value can
    /// carry it; the chat loop rejects a call whose arguments contain it before the skill runs.
    /// </summary>
    public static readonly string UnknownReferenceMarker = (char)0 + "UNKNOWN_PSEUDONYM:";

    public const string UnknownReferenceRejectedResult =
        "Rejected: an argument contains a pseudonym code that does not exist, so the call did NOT run. " +
        "Use only codes that appear in this conversation, exactly as written. If you do not know who is meant, " +
        "ask the user instead of guessing.";

    public const string ImageRefusedError =
        "Pseudonymisation is active and this request carries an image, which cannot be pseudonymised. The request was not sent.";

    public const string DictionaryUnavailableError =
        "Pseudonymisation is active but the pseudonym dictionary could not be loaded. The request was not sent.";

    public const string LegendHeader =
        "Privacy: names, phone numbers, e-mail addresses and personnel numbers of " +
        "known people are replaced by pseudonym codes such as MA-0017. Copy these codes exactly as written, in your " +
        "answer and in tool arguments; never translate, inflect, change or invent a code. Prefixes: MA employee, " +
        "KL customer, BE user, NAME a name several people share, TEL phone, MAIL e-mail. " +
        "A suffix names the part of the same person: -V first name, -N last name, -L maiden name, " +
        "-P personnel number; no suffix is the full name.";

    public const string LegendPeopleHeader = "People in this request:";

    public const string LegendEntryFormat = "- {0}: {1}";

    public const string LegendFemale = "female";

    public const string LegendMale = "male";

    public const string LegendOther = "gender not given";

    public const string LegendOrganisation = "organisation";
}
