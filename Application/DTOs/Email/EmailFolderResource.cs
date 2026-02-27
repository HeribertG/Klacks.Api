// Copyright (c) Heribert Gasparoli Private. All rights reserved.

namespace Klacks.Api.Application.DTOs.Email;

public class EmailFolderResource
{
    public Guid Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public string ImapFolderName { get; set; } = string.Empty;

    public int SortOrder { get; set; }

    public bool IsSystem { get; set; }

    public int UnreadCount { get; set; }

    public int TotalCount { get; set; }
}
