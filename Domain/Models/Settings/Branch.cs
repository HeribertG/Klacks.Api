using Klacks.Api.Domain.Common;

namespace Klacks.Api.Domain.Models.Settings;

public class Branch : BaseEntity
{
    public string Name { get; set; } = string.Empty;

    public string Address { get; set; } = string.Empty;

    public string Phone { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;
}
