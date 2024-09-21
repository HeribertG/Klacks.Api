using System.ComponentModel.DataAnnotations;

namespace Klacks.Api.Datas;


public class BaseEntity
{

    [Key]
    public Guid Id { get; set; }


    public DateTime? CreateTime { get; set; }

    public string? CurrentUserCreated { get; set; } = string.Empty;

    public string? CurrentUserDeleted { get; set; } = string.Empty;

    public string? CurrentUserUpdated { get; set; } = string.Empty;

    public DateTime? DeletedTime { get; set; }

    public bool IsDeleted { get; set; }

    public DateTime? UpdateTime { get; set; }
}
