// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Verifies that the configured ERP drop point (inbox) folder is reachable and writable, ensures it
/// exists (created if missing; there are no sub-folders -- files stay in the inbox and their import
/// state lives in the erp_import_file ledger). On a Docker installation it explains -- transparently, since
/// this check runs inside the container and cannot see the host's docker-compose.yml -- what is
/// needed for the folder to survive a container restart; on a native installation the folder is an ordinary
/// folder of the data root and the container remark is left out.
/// </summary>

using Klacks.Api.Application.Queries.ErpDropPoints;
using Klacks.Api.Domain.Attributes;
using Klacks.Api.Domain.Interfaces.Imports;
using Klacks.Api.Domain.Interfaces.Update;
using Klacks.Api.Domain.Models.Assistant;
using Klacks.Api.Domain.Models.Imports;
using Klacks.Api.Domain.Models.Update;
using Klacks.Api.Domain.Services.Assistant.Skills.Implementations;
using Klacks.Api.Domain.Services.Imports;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Skills;

[SkillImplementation("check_erp_drop_point_folder_health")]
public class CheckErpDropPointFolderHealthSkill : BaseSkillImplementation
{
    private const string VolumeExample = "api-erp-import:/app/ErpImport";
    private const string ServerComposeFileName = "docker-compose-server.yml";
    private const string OnPremComposeFileName = "deploy/onprem/docker-compose.yml";
    private const string LocalComposeFileName = "Klacks.Api/docker-compose.yml";

    private readonly IMediator _mediator;
    private readonly IObjectStorageService _objectStorageService;
    private readonly IInstallationIdentityProvider _installationIdentityProvider;

    public CheckErpDropPointFolderHealthSkill(
        IMediator mediator,
        IObjectStorageService objectStorageService,
        IInstallationIdentityProvider installationIdentityProvider)
    {
        _mediator = mediator;
        _objectStorageService = objectStorageService;
        _installationIdentityProvider = installationIdentityProvider;
    }

    public override async Task<SkillResult> ExecuteAsync(
        SkillExecutionContext context,
        Dictionary<string, object> parameters,
        CancellationToken cancellationToken = default)
    {
        var dropPoint = await _mediator.Send(new GetDefaultQuery(), cancellationToken);
        if (dropPoint == null)
        {
            return SkillResult.Error("No default drop point is configured.");
        }

        var normalizedPrefix = ErpImportStorageKeys.NormalizePrefix(dropPoint.BucketPrefix);
        var dropPointPath = _objectStorageService.ResolvePath(normalizedPrefix);
        var requiredPrefixes = new[] { normalizedPrefix };

        var health = await _objectStorageService.CheckHealthAsync(requiredPrefixes, cancellationToken);
        var isHealthy = health.RootDirectoryReady && health.IsWritable && health.Prefixes.All(p => p.Ready);

        var data = new
        {
            health.RootPath,
            DropPointPath = dropPointPath,
            health.RootDirectoryExisted,
            health.RootDirectoryReady,
            health.IsWritable,
            health.WriteTestError,
            Prefixes = health.Prefixes.Select(p => new { p.Prefix, p.Ready, p.Error }),
            IsHealthy = isHealthy,
        };

        return SkillResult.SuccessResult(data, BuildMessage(health, dropPointPath, _installationIdentityProvider.Get().Kind));
    }

    private static string BuildMessage(ObjectStorageHealthResult health, string dropPointPath, InstallationKind installationKind)
    {
        if (!health.RootDirectoryReady)
        {
            return $"Drop point folder '{dropPointPath}' does not exist and could not be created. " +
                   "Check the file system permissions of the account Klacks runs under.";
        }

        if (!health.IsWritable)
        {
            return $"Drop point folder '{dropPointPath}' exists but is not writable ({health.WriteTestError}). " +
                   "Check the file system permissions of the account Klacks runs under.";
        }

        var failedPrefixes = health.Prefixes.Where(p => !p.Ready).ToList();
        if (failedPrefixes.Count > 0)
        {
            var names = string.Join(", ", failedPrefixes.Select(p => p.Prefix));
            return $"The storage root is writable, but the drop point folder could not be created: {names}.";
        }

        var justCreatedWarning = health.RootDirectoryExisted
            ? string.Empty
            : $" NOTE: '{dropPointPath}' did not exist before this check and was just created -- " +
              "confirm this is really the path the delivering system writes to, a typo in RootPath would " +
              "otherwise create and report a healthy but unused folder.";

        var baseMessage =
            $"Drop point folder '{dropPointPath}' is in place and writable for Klacks. Delivered files stay in this folder; " +
            "their import state is tracked per file (use get_erp_import_status)." +
            justCreatedWarning;

        if (installationKind == InstallationKind.Native)
        {
            return baseMessage +
                   " This check only proves that Klacks itself can write there; the account the ERP export runs under " +
                   "needs its own write access to exactly this folder (get_erp_drop_point_settings explains how).";
        }

        return baseMessage +
               " This check runs inside the running container and cannot inspect the host's docker-compose.yml volume " +
               $"configuration. For files to survive a container restart, RootPath must be mounted as a named volume " +
               $"in docker-compose.yml -- example: `{VolumeExample}`. The production compose files " +
               $"({ServerComposeFileName}, {OnPremComposeFileName}) already configure this correctly; only the simple " +
               $"local {LocalComposeFileName} (not for production use) has no volume for it. An ERP on the host can only " +
               "write into the folder if a host folder is mounted onto it.";
    }
}
