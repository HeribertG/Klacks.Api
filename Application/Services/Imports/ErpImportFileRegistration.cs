// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

using Klacks.Api.Domain.Enums;
using Klacks.Api.Domain.Models.Imports;

namespace Klacks.Api.Application.Services.Imports;

/// <summary>
/// Everything ErpImportFileRegistrar needs to write one erp_import_file row for a stored object.
/// </summary>
/// <param name="ReceivedAt">Received time to record (e.g. the object's last write time for inbox files); null = now</param>
/// <param name="Status">Received (the default, the file is imported) or Failed (registered for an admin decision only)</param>
/// <param name="FailureReason">Reason stored with a Failed registration; ignored for Received</param>
public sealed record ErpImportFileRegistration(
    ErpDropPoint DropPoint,
    string StorageKey,
    string FileName,
    StoredObjectInfo StoredObject,
    ErpImportChannel Channel,
    string? UploadedBy,
    DateTime? ReceivedAt = null,
    ErpImportFileStatus Status = ErpImportFileStatus.Received,
    string? FailureReason = null);
