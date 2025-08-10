using Klacks.Api.Datas;
using Klacks.Api.Domain.Models.Authentification;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Klacks.Api.Domain.Models.Associations;

public class GroupVisibility : BaseEntity
{
    public required string AppUserId { get; set; }

    public virtual AppUser AppUser { get; set; } = null!;

    [Required]
    [ForeignKey("Group")]
    public Guid GroupId { get; set; }

    public virtual Group Group { get; set; } = null!;
}
