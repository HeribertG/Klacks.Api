// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace Klacks.Api.Domain.Models.Staffs;

public class ClientImage
{
    [Key]
    public Guid Id { get; set; }

    [Required]
    [ForeignKey("Client")]
    public Guid ClientId { get; set; }

    [JsonIgnore]
    public Client? Client { get; set; }

    [Required]
    public byte[] ImageData { get; set; } = Array.Empty<byte>();

    [Required]
    [StringLength(100)]
    public string ContentType { get; set; } = string.Empty;

    [StringLength(255)]
    public string? FileName { get; set; }

    public long FileSize { get; set; }

    public DateTime? CreateTime { get; set; }

    public DateTime? UpdateTime { get; set; }
}
