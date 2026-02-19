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
}
