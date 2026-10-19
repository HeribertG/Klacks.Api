// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Singleton identity of this API process for the ERP import file ledger ("MachineName:ProcessGuid").
/// Written to erp_import_file.claimed_by on claim and required by the heartbeat and finish guards, so
/// only the process that claimed a file can report on it. A fresh guid per process start means a
/// restarted process never mistakes an orphaned claim of its predecessor for its own.
/// </summary>
using Klacks.Api.Domain.Constants;

namespace Klacks.Api.Application.Services.Imports;

public sealed class ErpImportInstanceIdentity
{
    private const char Separator = ':';

    private const string ProcessGuidFormat = "N";

    public ErpImportInstanceIdentity()
        : this(Environment.MachineName, Guid.NewGuid())
    {
    }

    public ErpImportInstanceIdentity(string machineName, Guid processGuid)
    {
        var processPart = processGuid.ToString(ProcessGuidFormat);
        var maxMachineLength = ErpImportFileLimits.ClaimedByMaxLength - processPart.Length - 1;
        var machinePart = machineName.Length > maxMachineLength ? machineName[..maxMachineLength] : machineName;

        InstanceId = $"{machinePart}{Separator}{processPart}";
    }

    public string InstanceId { get; }
}
