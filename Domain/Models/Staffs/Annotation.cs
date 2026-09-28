// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

using Klacks.Api.Domain.Common;

namespace Klacks.Api.Domain.Models.Staffs;

public class Annotation : BaseEntity
{
    public Guid ClientId { get; set; }
    public virtual Client Client { get; set; } = null!;

    public string Note { get; set; } = string.Empty;



}
