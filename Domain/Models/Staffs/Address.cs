using Klacks.Api.Domain.Common;
using Klacks.Api.Domain.Enums;
using Newtonsoft.Json;
using System.ComponentModel.DataAnnotations;


namespace Klacks.Api.Domain.Models.Staffs;

public class Address : BaseEntity
{
    public Guid ClientId { get; set; }

    public virtual Client Client { get; set; } = null!;

    [Required]
    [DataType(DataType.Date)]
    public DateTime? ValidFrom { get; set; }

    [Required]
    public AddressTypeEnum Type { get; set; }

    public string AddressLine1 { get; set; } = string.Empty;

    public string AddressLine2 { get; set; } = string.Empty;

    public string Street { get; set; } = string.Empty;

    public string Street2 { get; set; } = string.Empty;

    public string Street3 { get; set; } = string.Empty;

    public string Zip { get; set; } = string.Empty;

    public string City { get; set; } = string.Empty;

    public string State { get; set; } = string.Empty;

    public string Country { get; set; } = string.Empty;



}
