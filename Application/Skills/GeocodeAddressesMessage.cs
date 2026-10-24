// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// The English answer of geocode_addresses for the model to relay in the user's language. It always states what
/// was written without asking (the owner's condition for the confirmation exception), what runs on in the
/// background, what could not be placed and what was skipped and why - and nothing about tools or skill names.
/// </summary>

using System.Globalization;
using System.Text;
using Klacks.Api.Application.DTOs.Geocoding;
using Klacks.Api.Domain.Constants;

namespace Klacks.Api.Application.Skills;

internal static class GeocodeAddressesMessage
{
    public const string CoordinateFormat = "0.#####";

    private const string NothingToDo =
        "No address without coordinates was found among the clients the user may see. Nothing was written.";

    private const string FilledIntro =
        "Coordinates were stored WITHOUT asking first (this is allowed because only empty coordinates are filled, " +
        "never overwritten). You MUST tell the user what was written: {0} address(es) now carry the approximate " +
        "position of their postcode and city:";

    private const string Queued =
        "{0} more address(es) are being located in the background; that takes a few minutes. Nothing else is needed.";

    private const string Deferred =
        "{0} more address(es) were not processed in this turn and the background geocoding does not run on this " +
        "installation; the user can ask again to continue.";

    private const string NotFound =
        "{0} address(es) could not be located from their postcode and city; their coordinates stay empty. " +
        "Suggest checking postcode and city.";

    private const string PrivacySkipped =
        "{0} employee address(es) were left out: this installation does not allow private employee addresses to be " +
        "sent to the map service. An administrator can allow it in the settings.";

    private const string Incomplete = "{0} address(es) have no postcode or city and cannot be located.";

    public static string Build(AddressGeocodingSelection selection, AddressGeocodingRunSummary summary)
    {
        var total = selection.Eligible.Count + selection.PrivacyBlocked.Count + selection.Incomplete.Count;
        if (total == 0)
        {
            return NothingToDo;
        }

        var text = new StringBuilder();
        if (summary.Filled.Count > 0)
        {
            text.AppendLine(Format(FilledIntro, summary.Filled.Count));
            foreach (var filled in summary.Filled.Take(AddressGeocodingLimits.AssistantReportedAddressLimit))
            {
                text.AppendLine(string.Format(
                    CultureInfo.InvariantCulture,
                    "- {0}, {1} {2}: {3}, {4}",
                    filled.Address.ClientName,
                    filled.Address.Zip,
                    filled.Address.City,
                    filled.Latitude.ToString(CoordinateFormat, CultureInfo.InvariantCulture),
                    filled.Longitude.ToString(CoordinateFormat, CultureInfo.InvariantCulture)));
            }
        }

        AppendIfAny(text, Queued, summary.Queued);
        AppendIfAny(text, Deferred, summary.Deferred);
        AppendIfAny(text, NotFound, summary.NotFound);
        AppendIfAny(text, PrivacySkipped, selection.PrivacyBlocked.Count);
        AppendIfAny(text, Incomplete, selection.Incomplete.Count);

        return text.Length == 0 ? NothingToDo : text.ToString().TrimEnd();
    }

    private static void AppendIfAny(StringBuilder text, string template, int count)
    {
        if (count > 0)
        {
            text.AppendLine(Format(template, count));
        }
    }

    private static string Format(string template, int count) => string.Format(CultureInfo.InvariantCulture, template, count);
}
