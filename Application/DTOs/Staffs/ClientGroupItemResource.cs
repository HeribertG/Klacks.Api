using System.ComponentModel.DataAnnotations;

namespace Klacks.Api.Application.DTOs.Staffs;

public class ClientGroupItemResource
{
    public Guid GroupId { get; set; }

    public Guid? ClientId { get; set; }

    public string GroupName { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    [DataType(DataType.Date)]
    public DateTime? ValidFrom { get; set; }

    [DataType(DataType.Date)]
    public DateTime? ValidUntil { get; set; }
}
