// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using System.ComponentModel.DataAnnotations;

namespace Klacks.Api.Domain.Models.Authentification;

public class RefreshToken
{
    [Key]
    public Guid Id { get; set; }
    public string AspNetUsersId { get; set; } = string.Empty;
    [MaxLength(100)]
    public string Token { get; set; } = string.Empty;
    public DateTime ExpiryDate { get; set; }
}
