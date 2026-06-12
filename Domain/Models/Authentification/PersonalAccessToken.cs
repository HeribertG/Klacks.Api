// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Domain.Common;

namespace Klacks.Api.Domain.Models.Authentification;

public class PersonalAccessToken : BaseEntity
{
    public string UserId { get; set; } = string.Empty;

    public virtual AppUser User { get; set; } = null!;

    public string Name { get; set; } = string.Empty;

    public string TokenHash { get; set; } = string.Empty;

    public string TokenPrefix { get; set; } = string.Empty;

    public DateTime? ExpiresAt { get; set; }

    public DateTime? LastUsedAt { get; set; }
}
