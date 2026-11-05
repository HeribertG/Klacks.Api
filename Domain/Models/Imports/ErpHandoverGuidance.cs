// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Install-dependent advice on how an ERP system hands order files to Klacks.
/// </summary>
/// <param name="Installation">Installation kind as a lower-case word: native or docker</param>
/// <param name="InstallationLabel">Plain words for the installation kind that an answer can use as they are: Windows office PC, office PC or Docker server</param>
/// <param name="RecommendedChannels">ErpHandoverChannels values, best fit for this installation first</param>
/// <param name="FolderDropRequirement">ErpFolderDropRequirements value naming what must be arranged before an ERP can write into the folder</param>
/// <param name="Advice">English prose the assistant can relay: folder access, alternatives, limits of this installation kind</param>
namespace Klacks.Api.Domain.Models.Imports;

public sealed record ErpHandoverGuidance(
    string Installation,
    string InstallationLabel,
    IReadOnlyList<string> RecommendedChannels,
    string FolderDropRequirement,
    string Advice);
