// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using System.ComponentModel.DataAnnotations;
using Klacks.Api.Domain.Common;

namespace Klacks.Api.Domain.Models.Bots;

public class KlacksBotToken : BaseEntity
{
    [Required]
    [MaxLength(200)]
    public string Name { get; set; } = string.Empty;

    public string TokenHash { get; set; } = string.Empty;

    public string TokenPrefix { get; set; } = string.Empty;

    public DateTime? ExpiresAt { get; set; }

    public DateTime? LastUsedAt { get; set; }
}
