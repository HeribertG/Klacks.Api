// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Junction table entry associating a qualification with a country ISO code.
/// </summary>
/// <param name="QualificationId">FK to the parent qualification</param>
/// <param name="CountryCode">ISO 3166-1 alpha-2 country code (e.g. "CH", "DE")</param>

using System.Text.Json.Serialization;

namespace Klacks.Api.Domain.Models.Staffs;

public class QualificationCountry
{
    public Guid QualificationId { get; set; }

    public string CountryCode { get; set; } = string.Empty;

    [JsonIgnore]
    public Qualification Qualification { get; set; } = null!;
}
