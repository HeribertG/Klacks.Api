// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using System.ComponentModel.DataAnnotations;
using Klacks.Api.Domain.Common;

namespace Klacks.Api.Domain.Models.Marketing;

/// <summary>
/// A demo request captured from the marketing site contact form.
/// Tracked for advertising lead measurement (70/30 campaign split).
/// </summary>
public class DemoRequest : BaseEntity
{
    [Required, MaxLength(200)]
    public required string Name { get; set; }

    [Required, EmailAddress, MaxLength(200)]
    public required string Email { get; set; }

    [MaxLength(200)]
    public string? Company { get; set; }

    [MaxLength(10)]
    public string? CountryCode { get; set; }

    [MaxLength(50)]
    public string? Industry { get; set; }

    [MaxLength(2000)]
    public string? Message { get; set; }

    /// <summary>
    /// UTM campaign identifier from the advertising link that brought the visitor.
    /// </summary>
    [MaxLength(200)]
    public string? UtmCampaign { get; set; }

    /// <summary>
    /// The marketing page URL where the form was submitted.
    /// </summary>
    [MaxLength(500)]
    public string? SourceUrl { get; set; }
}
