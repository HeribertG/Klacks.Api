// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Confirmation preview of start_software_update: when the autonomy gate holds the call, it asks the same preflight the
/// trigger uses and states the running and the target version, the release channel, the database copy of a release that
/// changes the schema and what the restart does to the people using Klacks. It refuses, so the gate issues no token, when
/// the trigger would refuse. The target version is the preview's binding (ConfirmationBindingKinds.SoftwareUpdate): the
/// confirmed replay installs exactly this version or nothing. It only reads.
/// </summary>
/// <param name="preflight">Decides whether the trigger would enqueue and which version it would take</param>

using Klacks.Api.Application.Constants;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Domain.Interfaces.Assistant;
using Klacks.Api.Domain.Models.Assistant;

namespace Klacks.Api.Application.Skills;

public class StartSoftwareUpdateConfirmationPreviewProvider : ISkillConfirmationPreviewProvider
{
    private const string DatabaseCopyNote = " This release changes the database structure, so a database copy is taken first.";
    private const string RefusalPrefix = "The update cannot be started: ";

    private readonly IUpdateTriggerPreflight _preflight;

    public StartSoftwareUpdateConfirmationPreviewProvider(IUpdateTriggerPreflight preflight)
    {
        _preflight = preflight;
    }

    public bool Supports(string skillName) =>
        string.Equals(skillName, StartSoftwareUpdateSkill.SkillName, StringComparison.OrdinalIgnoreCase);

    public async Task<SkillConfirmationPreview> BuildAsync(
        string skillName,
        SkillExecutionContext context,
        Dictionary<string, object> parameters,
        CancellationToken cancellationToken = default)
    {
        var preflight = await _preflight.CheckAsync(cancellationToken);
        if (preflight is not { RejectionReason: null, Manifest: { } manifest })
        {
            return SkillConfirmationPreview.Refuse(RefusalPrefix + (preflight.RejectionReason ?? UpdateReasons.NoManifest));
        }

        var databaseNote = manifest.ContainsMigrations ? DatabaseCopyNote : string.Empty;
        var text =
            $"Install Klacks {manifest.LatestVersion} (running {preflight.CurrentVersion}, channel {preflight.Channel})." +
            $"{databaseNote} {SoftwareUpdateNotices.Restart} {SoftwareUpdateNotices.AutomaticRestore} " +
            "Exactly this version is bound to the confirmation.";
        return SkillConfirmationPreview.Show(text, StartSoftwareUpdateSkill.BindingFor(manifest.LatestVersion));
    }
}
