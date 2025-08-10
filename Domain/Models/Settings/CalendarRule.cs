using Klacks.Api.Datas;
using System.ComponentModel.DataAnnotations;

namespace Klacks.Api.Domain.Models.Settings;

public class CalendarRule
{
    public string Country { get; set; } = string.Empty;

    public MultiLanguage? Description { get; set; } = null!;

    [Key]
    public Guid Id { get; set; }

    public bool IsMandatory { get; set; }

    public bool IsPaid { get; set; }

    public MultiLanguage? Name { get; set; } = null!;

    public string Rule { get; set; } = string.Empty;

    public string State { get; set; } = string.Empty;

    public string SubRule { get; set; } = string.Empty;
}
