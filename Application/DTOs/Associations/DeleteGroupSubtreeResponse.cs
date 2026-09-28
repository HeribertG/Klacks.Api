// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

namespace Klacks.Api.Application.DTOs.Associations;

public class DeleteGroupSubtreeResponse
{
    public string DeletedGroupName { get; set; } = string.Empty;

    /// <summary>The group itself plus every child removed with it.</summary>
    public int DeletedCount { get; set; }
}
