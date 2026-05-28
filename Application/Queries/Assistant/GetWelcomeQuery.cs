// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Application.DTOs.Assistant;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Queries.Assistant;

/// <summary>
/// Query for Klacksy's personalized welcome message and ranked suggestion list.
/// </summary>
/// <param name="Lang">ISO 639-1 language code (de/en/fr/it)</param>
/// <param name="LocalHour">User's local hour 0..23 — used for daypart selection</param>
/// <param name="Weekday">User's local weekday 0..6 (0=Sunday, ISO compatible)</param>
/// <param name="Route">Optional current router URL (e.g. /workplace/schedule)</param>
/// <param name="Latitude">Optional latitude for weather lookup</param>
/// <param name="Longitude">Optional longitude for weather lookup</param>
/// <param name="DisplayName">User's display name (taken from JWT username claim by the FE)</param>
/// <param name="ExcludeVariantIndex">Greeting variant the FE just rendered — handler avoids repeating it</param>
/// <param name="UserId">Resolved from JWT claims by controller</param>
/// <param name="UserRights">Resolved from JWT role/permission claims by controller</param>
public class GetWelcomeQuery : IRequest<WelcomeResource>
{
    public string Lang { get; set; } = "en";
    public int LocalHour { get; set; }
    public int Weekday { get; set; }
    public string? Route { get; set; }
    public double? Latitude { get; set; }
    public double? Longitude { get; set; }
    public string? DisplayName { get; set; }
    public int? ExcludeVariantIndex { get; set; }
    public string UserId { get; set; } = string.Empty;
    public List<string> UserRights { get; set; } = new();
}
