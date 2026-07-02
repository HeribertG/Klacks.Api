// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using System.ComponentModel.DataAnnotations;
using Klacks.Api.Domain.Common;

namespace Klacks.Api.Domain.Models.Imports;

public class ErpDropPoint : BaseEntity
{
    [Required]
    [MaxLength(200)]
    public string Name { get; set; } = string.Empty;

    [Required]
    [MaxLength(200)]
    public string SourceSystemId { get; set; } = string.Empty;

    [Required]
    [MaxLength(500)]
    public string BucketPrefix { get; set; } = string.Empty;

    public bool IsEnabled { get; set; } = true;

    public DateTime? LastPolledAt { get; set; }

    [MaxLength(1000)]
    public string? LastError { get; set; }
}
