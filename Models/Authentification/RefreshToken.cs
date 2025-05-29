using System.ComponentModel.DataAnnotations;

namespace Klacks.Api.Models.Authentification;

public class RefreshToken
{
    [Key]
    public Guid Id { get; set; }
    public string AspNetUsersId { get; set; } = string.Empty;
    [MaxLength(100)]
    public string Token { get; set; } = string.Empty;
    public DateTime ExpiryDate { get; set; }
}
