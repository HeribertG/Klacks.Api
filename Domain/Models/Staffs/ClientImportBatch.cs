// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Metadata of one committed employee import. The uploaded file itself is never stored; the unique
/// Token (issued by Parse) makes a repeated commit of the same import fail instead of creating the
/// employees twice. CreateTime and CurrentUserCreated record when and by whom it ran.
/// </summary>

using Klacks.Api.Domain.Common;
using Klacks.Api.Domain.Constants;
using System.ComponentModel.DataAnnotations;

namespace Klacks.Api.Domain.Models.Staffs;

public class ClientImportBatch : BaseEntity
{
    public Guid Token { get; set; }

    [StringLength(ClientImportLimits.MaxFileNameLength)]
    public string FileName { get; set; } = string.Empty;

    public int RowCount { get; set; }

    public int CreatedCount { get; set; }

    public int SkippedCount { get; set; }
}
