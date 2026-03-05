// Copyright (c) Heribert Gasparoli Private. All rights reserved.

namespace Klacks.Api.Application.DTOs.Email;

public class EmailGroupTreeNode
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public EmailGroupNodeType Type { get; set; }
    public int EmailCount { get; set; }
    public int UnreadCount { get; set; }
    public List<EmailGroupTreeNode> Children { get; set; } = [];
}
