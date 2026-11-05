// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Decides which hand-over channels fit an installation. A native installation (office PC) keeps its data folder
/// readable and writable for SYSTEM and Administrators only, so an ERP export needs an explicit grant on exactly the
/// drop point folder. A Docker installation keeps the folder inside the container (named volume), out of reach for an
/// ERP on the host unless a host folder is mounted. Docker cannot tell an own server from one hosted by the vendor, so
/// the advice names both cases.
/// </summary>
/// <param name="identity">Installation kind and runtime identifier of the running process</param>
/// <param name="folderPath">Absolute drop point folder as the Klacks server sees it</param>

using Klacks.Api.Domain.Constants;
using Klacks.Api.Domain.Models.Imports;
using Klacks.Api.Domain.Models.Update;

namespace Klacks.Api.Domain.Services.Imports;

public static class ErpHandoverAdvisor
{
    private const string NativeInstallation = "native";
    private const string DockerInstallation = "docker";
    private const string WindowsRuntimePrefix = "win";
    private const string DockerMountPoint = "/app/ErpImport";
    private const string DockerVolumeName = "api-erp-import";
    private const string ImportSettingsPage = "Settings > Import (expert mode, administrators)";
    private const string TokenSentence = "an ERP import token created on that page (tokens exist only there)";

    public static ErpHandoverGuidance Advise(InstallationIdentity identity, string folderPath)
    {
        return identity.Kind == InstallationKind.Native
            ? AdviseNative(identity, folderPath)
            : AdviseDocker(folderPath);
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
            NativeInstallation,
            [ErpHandoverChannels.FolderDrop, ErpHandoverChannels.HttpsPush, ErpHandoverChannels.AdminUpload],
            ErpFolderDropRequirements.GrantWriteAccessOnFolder,
            advice);
    }

    private static ErpHandoverGuidance AdviseDocker(string folderPath)
    {
        var advice =
            $"Docker installation. The folder '{folderPath}' lies inside the Klacks container (named volume {DockerVolumeName} " +
            $"on {DockerMountPoint}), so an ERP running on the host or elsewhere cannot write into it unless an administrator " +
            $"mounts a host folder onto {DockerMountPoint} in the compose file. On an own server: mount a host folder, or let " +
            $"the ERP push over HTTPS with {TokenSentence}, or upload on {ImportSettingsPage}. If the vendor hosts this " +
            "installation there is no access to server folders at all: use the push token or the upload.";

        return new ErpHandoverGuidance(
            DockerInstallation,
            [ErpHandoverChannels.HttpsPush, ErpHandoverChannels.AdminUpload, ErpHandoverChannels.FolderDrop],
            ErpFolderDropRequirements.MountHostFolder,
            advice);
    }
}
