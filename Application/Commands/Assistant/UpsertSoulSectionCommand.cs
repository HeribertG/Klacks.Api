// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Commands.Assistant;

public class UpsertSoulSectionCommand : IRequest<object>
{
    public Guid AgentId { get; set; }
    public string SectionType { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public int? SortOrder { get; set; }
    public string? UserId { get; set; }
}
