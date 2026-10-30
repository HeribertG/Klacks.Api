// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Deterministic advice on "should I update now", derived only from the trigger's own preflight, the update history, the
/// in-memory run counts and the assistant connections. Whether the update can start is the preflight's decision and is
/// never recomputed here; this class only explains it and adds what speaks against starting: a failed last attempt, a
/// schema change, running planning work and other connected users. Only a last rollback that failed advises against a
/// start the trigger would accept. Pure and static, so every rule is pinned by a unit test; the model only relays it.
/// </summary>
/// <param name="facts">The gathered facts</param>

using System.Globalization;
using Klacks.Api.Application.Constants;
using Klacks.Api.Application.DTOs.Update;
using Klacks.Api.Domain.Constants;
using Klacks.Api.Domain.Models.Update;

namespace Klacks.Api.Application.Services.Update;

public static class SoftwareUpdateAdvisor
{
    public const string NotMeasured =
        "Not visible to this advice: whether the updater service is running, unsaved work in open browsers, signed-in " +
        "people without an open Klacks tab and a broadcast still sending through the messaging plugin.";

    private const string ConfirmationNotice =
        "Nothing has been installed; the administrator decides, and the system asks for one confirmation first.";

    public static SoftwareUpdateEvaluation Evaluate(SoftwareUpdateFacts facts)
    {
        var findings = Analyse(facts);
        var verdict = DecideVerdict(facts, findings);
        var preflight = facts.Preflight;

        return new SoftwareUpdateEvaluation(
            preflight.CurrentVersion.ToString(),
            preflight.Manifest?.LatestVersion,
            preflight.Channel.ToString(),
            preflight.Installation.Kind.ToString(),
            preflight.RejectionReason is null,
            facts.RunningWork,
            facts.OtherConnectedUsers,
            verdict,
            findings.OrderByDescending(finding => finding.IsBlocking).ToList(),
            BuildRecommendation(facts, verdict, findings),
            NotMeasured);
    }

    private static List<SoftwareUpdateFinding> Analyse(SoftwareUpdateFacts facts)
    {
        var findings = new List<SoftwareUpdateFinding>();
        var preflight = facts.Preflight;

        if (RejectionFinding(facts) is { } rejection)
        {
            findings.Add(rejection);
        }

        AddActiveOperationFindings(facts, findings);
        AddLastRollbackFinding(facts, findings);

        if (preflight.RejectionReason is null)
        {
            AddStartFindings(facts, findings);
        }

        return findings;
    }

    private static SoftwareUpdateFinding? RejectionFinding(SoftwareUpdateFacts facts)
    {
        var preflight = facts.Preflight;
        var current = preflight.CurrentVersion.ToString();
        var latest = preflight.Manifest?.LatestVersion;

        return preflight.RejectionReason switch
        {
            null or UpdateReasons.OperationInProgress => null,
            UpdateReasons.NoManifest when !facts.SourceConfigured => Blocking(
                SoftwareUpdateFindingCode.UpdateSourceNotConfigured,
                "This installation has no address for release announcements, so no version is ever offered. The address is set during deployment."),
            UpdateReasons.NoManifest => Blocking(
                SoftwareUpdateFindingCode.ManifestUnavailable,
                "The release information could not be read just now, so nothing can be said about a newer version."),
            UpdateReasons.RequiresIntermediate => Blocking(
                SoftwareUpdateFindingCode.IntermediateVersionRequired,
                $"Version {latest} cannot be installed directly from {current}; an intermediate version is required first."),
            UpdateReasons.UpToDate when preflight.Availability?.Status == UpdateAvailabilityStatus.ManifestInvalid => Blocking(
                SoftwareUpdateFindingCode.ManifestInvalid,
                "The release information names an unreadable version, so no update can be offered from it."),
            UpdateReasons.UpToDate => new SoftwareUpdateFinding(
                SoftwareUpdateFindingCode.AlreadyCurrent,
                $"Version {current} is the newest one on the {preflight.Channel} channel.",
                false),
            UpdateReasons.NoArtifactForInstallation => Blocking(
                SoftwareUpdateFindingCode.NoArtifactForInstallation,
                $"Version {latest} carries no signed package for this {preflight.Installation.Kind} installation ({preflight.Installation.RuntimeIdentifier})."),
            _ => Blocking(SoftwareUpdateFindingCode.TriggerRefused, preflight.RejectionReason),
        };
    }

    private static void AddActiveOperationFindings(SoftwareUpdateFacts facts, List<SoftwareUpdateFinding> findings)
    {
        if (facts.Preflight.ActiveOperation is not { } active)
        {
            return;
        }

        findings.Add(Blocking(
            SoftwareUpdateFindingCode.OperationActive,
            $"Another operation is in the queue ({active.OperationType}, {active.Status}), and only one can run at a time."));

        var waited = facts.NowUtc - active.RequestedAt;
        if (active is { Status: UpdateOperationStatus.Pending, StartedAt: null }
            && waited >= TimeSpan.FromMinutes(SoftwareUpdateAdviceLimits.UnclaimedOperationMinutes))
        {
            var minutes = ((int)waited.TotalMinutes).ToString(CultureInfo.InvariantCulture);
            findings.Add(new SoftwareUpdateFinding(
                SoftwareUpdateFindingCode.OperationNotPickedUp,
                $"That operation has been waiting for {minutes} minutes without being picked up. A waiting operation can be cancelled on the Software Updates card.",
                false));
        }
    }

    private static void AddLastRollbackFinding(SoftwareUpdateFacts facts, List<SoftwareUpdateFinding> findings)
    {
        if (facts.LastOperation is { Status: UpdateOperationStatus.RollbackFailed } last)
        {
            findings.Add(Blocking(
                SoftwareUpdateFindingCode.LastRollbackFailed,
                $"The last {last.OperationType} (target {last.TargetVersion}) ended with a failed return to the previous version, so the installation may be inconsistent. Check that first."));
        }
    }

    private static void AddStartFindings(SoftwareUpdateFacts facts, List<SoftwareUpdateFinding> findings)
    {
        var preflight = facts.Preflight;
        var latest = preflight.Manifest?.LatestVersion;

        findings.Add(new SoftwareUpdateFinding(
            SoftwareUpdateFindingCode.UpdateAvailable,
            $"Version {latest} is available on the {preflight.Channel} channel; running is {preflight.CurrentVersion}.",
            false));

        if (facts.LastOperation is { OperationType: UpdateOperationType.Update, Status: not UpdateOperationStatus.RollbackFailed } last
            && UpdateTriggerPreflight.IsFailedAttempt(last.Status)
            && string.Equals(last.TargetVersion, latest, StringComparison.Ordinal))
        {
            findings.Add(new SoftwareUpdateFinding(
                SoftwareUpdateFindingCode.LastAttemptFailed,
                $"The last attempt at version {latest} ended as {last.Status}; starting it again repeats that attempt.",
                false));
        }

        if (preflight.Channel != UpdateChannel.Stable)
        {
            findings.Add(new SoftwareUpdateFinding(
                SoftwareUpdateFindingCode.BetaChannel,
                $"This installation follows the {preflight.Channel} channel, not Stable.",
                false));
        }

        if (preflight.Manifest is { ContainsMigrations: true })
        {
            findings.Add(new SoftwareUpdateFinding(
                SoftwareUpdateFindingCode.ContainsMigrations,
                "This release changes the database structure: a database copy is taken first, and the copy is restored if the new version fails.",
                false));
        }

        if (facts.RunningWork.Any)
        {
            var work = facts.RunningWork;
            findings.Add(new SoftwareUpdateFinding(
                SoftwareUpdateFindingCode.RunningWork,
                $"Planning work is running and a restart ends it: autofill runs {work.AutofillRuns}, wizard jobs {work.WizardJobs}, harmonizer jobs {work.HarmonizerJobs}, holistic harmonizer jobs {work.HolisticHarmonizerJobs}. These counts can overlap.",
                false));
        }

        if (facts.OtherConnectedUsers > 0)
        {
            findings.Add(new SoftwareUpdateFinding(
                SoftwareUpdateFindingCode.OtherUsersConnected,
                $"{facts.OtherConnectedUsers} other user(s) have Klacks open and would be disconnected without warning.",
                false));
        }
    }

    private static SoftwareUpdateVerdict DecideVerdict(SoftwareUpdateFacts facts, List<SoftwareUpdateFinding> findings)
    {
        var rejection = facts.Preflight.RejectionReason;
        var blocked = findings.Any(finding => finding.IsBlocking);

        if (rejection is null)
        {
            if (blocked)
            {
                return SoftwareUpdateVerdict.NotRecommended;
            }

            var cautious = findings.Any(finding => finding.Code != SoftwareUpdateFindingCode.UpdateAvailable);
            return cautious ? SoftwareUpdateVerdict.StartWithCaution : SoftwareUpdateVerdict.StartNow;
        }

        return !blocked && rejection == UpdateReasons.UpToDate
            ? SoftwareUpdateVerdict.NothingToInstall
            : SoftwareUpdateVerdict.CannotStart;
    }

    private static string BuildRecommendation(
        SoftwareUpdateFacts facts, SoftwareUpdateVerdict verdict, List<SoftwareUpdateFinding> findings)
    {
        var latest = facts.Preflight.Manifest?.LatestVersion;
        var firstBlocker = findings.FirstOrDefault(finding => finding.IsBlocking)?.Statement;

        return verdict switch
        {
            SoftwareUpdateVerdict.StartNow =>
                $"Version {latest} can be installed and nothing measured speaks against doing it now. {SoftwareUpdateNotices.Restart} {ConfirmationNotice}",
            SoftwareUpdateVerdict.StartWithCaution =>
                $"Version {latest} can be installed, but weigh the listed points first; a quieter moment may be better. {SoftwareUpdateNotices.Restart} {ConfirmationNotice}",
            SoftwareUpdateVerdict.NotRecommended =>
                $"Better not to start the update now. {firstBlocker}",
            SoftwareUpdateVerdict.NothingToInstall =>
                $"Nothing to install: version {facts.Preflight.CurrentVersion} is the newest one on the {facts.Preflight.Channel} channel.",
            _ => $"The update cannot be started now. {firstBlocker}",
        };
    }

    private static SoftwareUpdateFinding Blocking(SoftwareUpdateFindingCode code, string statement) =>
        new(code, statement, true);
}
