// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Domain.Common;

namespace Klacks.Api.Domain.Models.Email;

public class EmailFolder : BaseEntity
{
    public string Name { get; set; } = string.Empty;

    public string ImapFolderName { get; set; } = string.Empty;

    public int SortOrder { get; set; }

    public bool IsSystem { get; set; }
}
