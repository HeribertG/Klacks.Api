using Klacks.Api.Domain.Models.Associations;

namespace Klacks.Api.Application.DTOs.Filter;

public class TruncatedGroup : BaseTruncatedResult
{
    public ICollection<Group> Groups { get; set; } = null!;
}
