// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using System.ComponentModel.DataAnnotations.Schema;

namespace Klacks.Api.Infrastructure.KnowledgeIndex.Domain;

public sealed class KnowledgeEntry
{
    public Guid Id { get; set; }
    public KnowledgeEntryKind Kind { get; set; }
    public string SourceId { get; set; } = string.Empty;
    public string Text { get; set; } = string.Empty;
    public byte[] TextHash { get; set; } = Array.Empty<byte>();

    [NotMapped]
    public float[] Embedding { get; set; } = Array.Empty<float>();

    public string? RequiredPermission { get; set; }
    public string? ExposedEndpointKey { get; set; }
    public DateTime UpdatedAt { get; set; }
}
