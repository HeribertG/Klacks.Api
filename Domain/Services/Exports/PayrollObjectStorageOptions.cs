// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Storage settings for the stored payroll export artifacts. The root is deliberately separate from the ERP drop zone
/// (ErpObjectStorage) so that an external ERP or SFTP user with access to the drop zone never sees wage files.
/// </summary>
/// <param name="RootPath">Directory below which the artifacts are stored; a relative path is resolved against the application base directory</param>
namespace Klacks.Api.Domain.Services.Exports;

public class PayrollObjectStorageOptions
{
    public const string SectionName = "PayrollObjectStorage";

    public const string DefaultRootPath = "PayrollExports";

    public string RootPath { get; set; } = DefaultRootPath;
}