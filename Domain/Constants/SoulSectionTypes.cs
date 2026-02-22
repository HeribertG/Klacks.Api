// Copyright (c) Heribert Gasparoli Private. All rights reserved.

namespace Klacks.Api.Domain.Constants;

public static class SoulSectionTypes
{
    public const string Identity = "identity";
    public const string Personality = "personality";
    public const string Tone = "tone";
    public const string Boundaries = "boundaries";
    public const string CommunicationStyle = "communication_style";
    public const string Values = "values";
    public const string GroupBehavior = "group_behavior";
    public const string UserContext = "user_context";
    public const string DomainExpertise = "domain_expertise";
    public const string ErrorHandling = "error_handling";

    public static readonly string[] All =
    [
        Identity, Personality, Tone, Boundaries,
        CommunicationStyle, Values, GroupBehavior,
        UserContext, DomainExpertise, ErrorHandling
    ];

    public static int GetDefaultSortOrder(string sectionType) => sectionType switch
    {
        Identity => 0,
        Personality => 1,
        Tone => 2,
        Boundaries => 3,
        CommunicationStyle => 4,
        Values => 5,
        DomainExpertise => 6,
        ErrorHandling => 7,
        UserContext => 8,
        GroupBehavior => 9,
        _ => 99
    };
}
