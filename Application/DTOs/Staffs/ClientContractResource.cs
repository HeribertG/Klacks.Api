// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Application.DTOs.Associations;
using System.ComponentModel.DataAnnotations;

namespace Klacks.Api.Application.DTOs.Staffs;

public class ClientContractResource
{
    public Guid Id { get; set; }

    [Required]
    public Guid ClientId { get; set; }

    [Required]
    public Guid ContractId { get; set; }

    public ContractResource? Contract { get; set; }

    [Required]
    public DateOnly FromDate { get; set; }

    public DateOnly? UntilDate { get; set; }

    public bool IsActive { get; set; }
}
