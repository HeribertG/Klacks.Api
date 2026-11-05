// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Decides which hand-over channels fit an installation. A native installation (office PC) keeps its data folder
/// readable and writable for SYSTEM and Administrators only, so an ERP export needs an explicit grant on exactly the
/// drop point folder. A Docker installation binds a host folder onto the storage root (standard compose files:
/// ./erp-import next to docker-compose.yml). When the compose file reports that host folder, the advice names it and
/// the folder drop comes first; otherwise the advice names the container path and asks to check the mount. Docker
/// cannot tell an own server from one hosted by the vendor, so the Docker advice names both cases.
/// </summary>
/// <param name="identity">Installation kind and runtime identifier of the running process</param>
/// <param name="folderPath">Absolute drop point folder as the Klacks server sees it (inside the container on Docker)</param>
/// <param name="hostFolderPath">Docker only: the same folder as seen on the host, null when the compose file does not report it</param>

using Klacks.Api.Domain.Constants;
using Klacks.Api.Domain.Models.Imports;
using Klacks.Api.Domain.Models.Update;

namespace Klacks.Api.Domain.Services.Imports;

public static class ErpHandoverAdvisor
{
    private const string WindowsRuntimePrefix = "win";
    private const string DockerMountPoint = "/app/ErpImport";
    private const string DefaultHostFolder = "./erp-import";
    private const string HostFolderVariable = "KLACKS_ERP_IMPORT_HOST_DIR";
    private const string ImportSettingsPage = "Settings > Import (expert mode, administrators)";
    private const string TokenSentence = "an ERP import token created on that page (tokens exist only there)";
    private const string VendorHostedSentence =
        "If the vendor hosts this installation there is no access to server folders at all: use the push token or the upload.";

    public static ErpHandoverGuidance Advise(InstallationIdentity identity, string folderPath, string? hostFolderPath = null)
    {
        if (identity.Kind == InstallationKind.Native)
        {
            return AdviseNative(identity, folderPath);
        }

        return string.IsNullOrWhiteSpace(hostFolderPath)
            ? AdviseDockerWithoutHostFolder(folderPath)
            : AdviseDockerWithHostFolder(folderPath, hostFolderPath);
    }

    private static ErpHandoverGuidance AdviseNative(InstallationIdentity identity, string folderPath)
    {
        var isWindows = identity.RuntimeIdentifier.StartsWith(WindowsRuntimePrefix, StringComparison.OrdinalIgnoreCase);
        var access = isWindows
            ? "The Klacks data folder is restricted to SYSTEM and Administrators, so an administrator must first grant the " +
              "Windows account the ERP export runs under the Modify right (German Windows: Aendern) on exactly this folder and " +
              "on nothing above it (not on the whole Klacks data folder); without that grant the export cannot write there."
            : "An administrator must make sure the account the ERP export runs under can write into exactly this folder.";

        var advice =
            $"Native installation (office PC). Preferred: the ERP export writes its XML files into '{folderPath}'. {access} " +
            "The folder is created by the first import run or by check_erp_drop_point_folder_health if it does not exist yet. " +
            $"Alternatives: an administrator uploads the file on {ImportSettingsPage} (imports at once), or the ERP pushes " +
            $"over HTTPS with {TokenSentence}.";

        return new ErpHandoverGuidance(
            ErpHandoverInstallations.Native,
            [ErpHandoverChannels.FolderDrop, ErpHandoverChannels.HttpsPush, ErpHandoverChannels.AdminUpload],
            ErpFolderDropRequirements.GrantWriteAccessOnFolder,
            advice);
    }

    private static ErpHandoverGuidance AdviseDockerWithHostFolder(string folderPath, string hostFolderPath)
    {
        var advice =
            $"Docker installation. The drop point folder is the server (host) folder '{hostFolderPath}'; inside the Klacks " +
            $"container it appears as '{folderPath}'. An ERP running on the server writes its XML files into the host folder; " +
            "an administrator must make sure the account the ERP export runs under can write into exactly that folder " +
            "(Klacks itself writes there as the container user appuser). " +
            $"Alternatives: the ERP pushes over HTTPS with {TokenSentence}, or an administrator uploads on {ImportSettingsPage}. " +
            VendorHostedSentence;

        return new ErpHandoverGuidance(
            ErpHandoverInstallations.Docker,
            [ErpHandoverChannels.FolderDrop, ErpHandoverChannels.HttpsPush, ErpHandoverChannels.AdminUpload],
            ErpFolderDropRequirements.GrantWriteAccessOnHostFolder,
            advice);
    }

    private static ErpHandoverGuidance AdviseDockerWithoutHostFolder(string folderPath)
    {
        var advice =
            $"Docker installation. The folder '{folderPath}' is the path inside the Klacks container; this installation does " +
            $"not report which server (host) folder is mounted onto {DockerMountPoint}. The standard compose files bind the " +
            $"host folder {DefaultHostFolder} next to docker-compose.yml (overridable with {HostFolderVariable}); an ERP on " +
            $"the server can only write into the drop point folder through such a mounted host folder. On an own server: check " +
            $"the mount in the compose file, or let the ERP push over HTTPS with {TokenSentence}, or upload on " +
            $"{ImportSettingsPage}. {VendorHostedSentence}";

        return new ErpHandoverGuidance(
            ErpHandoverInstallations.Docker,
            [ErpHandoverChannels.HttpsPush, ErpHandoverChannels.AdminUpload, ErpHandoverChannels.FolderDrop],
            ErpFolderDropRequirements.MountHostFolder,
            advice);
    }
}
