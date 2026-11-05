// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Verifies that the configured ERP drop point (inbox) folder is reachable and writable, ensures it
/// exists (created if missing; there are no sub-folders -- files stay in the inbox and their import
/// state lives in the erp_import_file ledger). On a Docker installation it explains -- transparently, since
/// this check runs inside the container and cannot see the host's docker-compose.yml -- the host folder that must be
/// bound onto the storage root (named when the compose file reports it); on a native installation the folder is an ordinary
/// folder of the data root and the container remark is left out.
/// </summary>

using Klacks.Api.Application.Queries.ErpDropPoints;
using Klacks.Api.Application.Services.Imports;
using Klacks.Api.Domain.Attributes;
using Klacks.Api.Domain.Constants;
using Klacks.Api.Domain.Interfaces.Imports;
using Klacks.Api.Domain.Models.Assistant;
using Klacks.Api.Domain.Models.Imports;
using Klacks.Api.Domain.Services.Assistant.Skills.Implementations;
using Klacks.Api.Domain.Services.Imports;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Skills;

[SkillImplementation("check_erp_drop_point_folder_health")]
public class CheckErpDropPointFolderHealthSkill : BaseSkillImplementation
{
    private const string HostFolderMountExample = "${KLACKS_ERP_IMPORT_HOST_DIR:-./erp-import}:/app/ErpImport";
    private const string ServerComposeFileName = "docker-compose-server.yml";
    private const string OnPremComposeFileName = "deploy/onprem/docker-compose.yml";
    private const string LocalComposeFileName = "Klacks.Api/docker-compose.yml";

    private readonly IMediator _mediator;
    private readonly IObjectStorageService _objectStorageService;
    private readonly ErpDropFolderResolver _folderResolver;

    public CheckErpDropPointFolderHealthSkill(
        IMediator mediator,
        IObjectStorageService objectStorageService,
        ErpDropFolderResolver folderResolver)
    {
        _mediator = mediator;
        _objectStorageService = objectStorageService;
        _folderResolver = folderResolver;
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
        var folder = _folderResolver.Resolve(dropPoint.BucketPrefix);
        var dropPointPath = folder.FolderPath;
        var requiredPrefixes = new[] { normalizedPrefix };

        var health = await _objectStorageService.CheckHealthAsync(requiredPrefixes, cancellationToken);
        var isHealthy = health.RootDirectoryReady && health.IsWritable && health.Prefixes.All(p => p.Ready);

        var data = new
        {
            health.RootPath,
            DropPointPath = dropPointPath,
            folder.HostFolderPath,
            health.RootDirectoryExisted,
            health.RootDirectoryReady,
            health.IsWritable,
            health.WriteTestError,
            Prefixes = health.Prefixes.Select(p => new { p.Prefix, p.Ready, p.Error }),
            IsHealthy = isHealthy,
        };

        return SkillResult.SuccessResult(data, BuildMessage(health, folder));
    }

    private static string BuildMessage(ObjectStorageHealthResult health, ErpDropFolder folder)
    {
        var dropPointPath = folder.FolderPath;
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

        if (folder.Guidance.Installation == ErpHandoverInstallations.Native)
        {
            return baseMessage +
                   " This check only proves that Klacks itself can write there; the account the ERP export runs under " +
                   "needs its own write access to exactly this folder (get_erp_drop_point_settings explains how).";
        }

        var hostFolder = folder.HostFolderPath == null
            ? " This installation does not report which host folder is mounted there (ErpObjectStorage__HostPathHint is not set)."
            : $" On the host this is the folder '{folder.HostFolderPath}'.";

        return baseMessage + hostFolder +
               " This check runs inside the running container and cannot inspect the host's docker-compose.yml. For files to " +
               "survive a container restart and for an ERP on the host to reach them, a host folder must be bound onto the " +
               $"storage root -- the standard compose files ({ServerComposeFileName}, {OnPremComposeFileName}) bind " +
               $"`{HostFolderMountExample}`; only the simple local {LocalComposeFileName} (not for production use) has no " +
               "mount for it. The account the ERP export runs under needs write access to exactly that host folder.";
    }
}
