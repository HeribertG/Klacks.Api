// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

using Klacks.Api.Domain.Models.Associations;

namespace Klacks.Api.Domain.DTOs.Filter;

public class TruncatedGroup : BaseTruncatedResult
{
    public ICollection<Group> Groups { get; set; } = null!;
}
