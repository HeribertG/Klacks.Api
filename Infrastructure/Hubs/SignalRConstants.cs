// Copyright (c) Heribert Gasparoli Private. All rights reserved.

namespace Klacks.Api.Infrastructure.Hubs;

public static class SignalRConstants
{
    public static class HubMethods
    {
        public const string JoinScheduleGroup = "JoinScheduleGroup";
        public const string LeaveScheduleGroup = "LeaveScheduleGroup";
        public const string JoinClientGroup = "JoinClientGroup";
        public const string LeaveClientGroup = "LeaveClientGroup";
        public const string JoinClientGroups = "JoinClientGroups";
        public const string LeaveClientGroups = "LeaveClientGroups";
        public const string GetConnectionId = "GetConnectionId";
    }

    public static class Groups
    {
        public const string NullAnalyseToken = "null";

        public static string Schedule(string startDate, string endDate, string? analyseToken)
            => $"schedule_{startDate}_{endDate}_{(string.IsNullOrWhiteSpace(analyseToken) ? NullAnalyseToken : analyseToken)}";

        public static string Schedule(DateOnly startDate, DateOnly endDate, Guid? analyseToken)
            => $"schedule_{startDate:yyyy-MM-dd}_{endDate:yyyy-MM-dd}_{(analyseToken?.ToString() ?? NullAnalyseToken)}";

        public static string Client(string clientId)
            => $"client_{clientId}";

        public static string Client(Guid clientId)
            => $"client_{clientId}";
    }

    public const string HubPath = "/hubs/work-notifications";
    public const string AssistantHubPath = "/hubs/assistant-notifications";
    public const string EmailHubPath = "/hubs/email-notifications";
    public const string WizardHubPath = "/hubs/wizard";
    public const string HarmonizerHubPath = "/hubs/harmonizer";

    public static class WizardGroups
    {
        public static string WizardJob(Guid jobId) => $"wizard-{jobId:N}";
    }

    public static class HarmonizerGroups
    {
        public static string HarmonizerJob(Guid jobId) => $"harmonizer-{jobId:N}";
    }

    public static class AssistantEvents
    {
        public const string ProactiveMessage = "ProactiveMessage";
        public const string OnboardingPrompt = "OnboardingPrompt";
    }
}
