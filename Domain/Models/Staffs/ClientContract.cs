// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Domain.Common;
using Klacks.Api.Domain.Models.Associations;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Klacks.Api.Domain.Models.Staffs;

public class ClientContract : BaseEntity
{
    [Required]
    [ForeignKey("Client")]
    public Guid ClientId { get; set; }

    public virtual Client Client { get; set; } = null!;

    [Required]
    [ForeignKey("Contract")]
    public Guid ContractId { get; set; }

    public virtual Contract Contract { get; set; } = null!;

    [Required]
    public DateOnly FromDate { get; set; }

    public DateOnly? UntilDate { get; set; }

    public bool IsActive { get; set; }
}
