using Klacks.Api.Datas;
using Klacks.Api.Models.Authentification;

namespace Klacks.Api.Models.Associations;

public class GroupVisibility : BaseEntity
{
    public required string AppUserId { get; set; }

    public virtual AppUser AppUser { get; set; } = null!;

    public Guid GroupId { get; set; }

    public virtual Group Group { get; set; } = null!;
}
