// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// SHA-256 fingerprints of an analysis. The report fingerprint covers the report findings
/// (GroupingFinding.IsReportFinding: F1, F5, F6 without removal, F7) and serves as dedup key of the inbox
/// entry; the plan fingerprint additionally covers the proposals and the analysed subtree and guards apply against
/// stale state. Neither contains the period, so a new day or week alone does not change them. Lines are
/// sorted ordinally, so the order of findings does not matter.
/// </summary>

using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using Klacks.Api.Application.DTOs.Grouping;
using Klacks.Api.Domain.Constants;

namespace Klacks.Api.Application.Services.Grouping;

public static class GroupingFingerprint
{
    private const char Separator = '|';
    private const char LineSeparator = '\n';
    private const string FindingPrefix = "F";
    private const string ProposalPrefix = "P";
    private const string ScopePrefix = "S";
    private const string GuidFormat = "N";

    public static string ForReport(IEnumerable<GroupingFinding> findings) =>
        Hash(ReportLines(findings));

    public static string ForPlan(Guid? scopeGroupId, IEnumerable<GroupingFinding> findings, IEnumerable<GroupingProposal> proposals) =>
        Hash(ReportLines(findings)
            .Concat(proposals.Select(ProposalLine))
            .Append(string.Join(Separator, ScopePrefix, Format(scopeGroupId))));

    public static string Shorten(string fingerprint) =>
        fingerprint[..GroupingFeasibilityDefaults.FingerprintDisplayLength];

    public static bool Matches(string fingerprint, string? supplied)
    {
        var candidate = supplied?.Trim().ToLowerInvariant();
        return candidate is not null
            && candidate.Length >= GroupingFeasibilityDefaults.FingerprintDisplayLength
            && fingerprint.StartsWith(candidate, StringComparison.Ordinal);
    }

    private static IEnumerable<string> ReportLines(IEnumerable<GroupingFinding> findings) =>
        findings.Where(GroupingFinding.IsReportFinding).Select(FindingLine);

    private static string FindingLine(GroupingFinding finding) => string.Join(
        Separator,
        FindingPrefix + ((int)finding.Code).ToString(CultureInfo.InvariantCulture),
        Format(finding.GroupId),
        Format(finding.ShiftId),
        Format(finding.ClientId),
        finding.Reason is null ? string.Empty : ((int)finding.Reason.Value).ToString(CultureInfo.InvariantCulture),
        finding.Weekday is null ? string.Empty : ((int)finding.Weekday.Value).ToString(CultureInfo.InvariantCulture),
        finding.Demand?.ToString(CultureInfo.InvariantCulture) ?? string.Empty,
        finding.Supply?.ToString(CultureInfo.InvariantCulture) ?? string.Empty);

    private static string ProposalLine(GroupingProposal proposal) => string.Join(
        Separator,
        ProposalPrefix + ((int)proposal.Kind).ToString(CultureInfo.InvariantCulture),
        Format(proposal.GroupId),
        proposal.NewGroupKey ?? string.Empty,
        Format(proposal.ClientId),
        Format(proposal.ShiftId));

    private static string Format(Guid? id) => id?.ToString(GuidFormat, CultureInfo.InvariantCulture) ?? string.Empty;

    private static string Hash(IEnumerable<string> lines)
    {
        var canonical = string.Join(LineSeparator, lines.OrderBy(line => line, StringComparer.Ordinal));
        return Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(canonical)));
    }
}
