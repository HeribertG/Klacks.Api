// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Placeholder names of the focus prompt templates, i.e. the keys of WelcomeFocusResource
/// PromptParams. They match the {{...}} slots in the translation files one to one.
/// </summary>

namespace Klacks.Api.Application.Constants;

public static class WelcomeFocusParamKeys
{
    public const string Group = "group";
    public const string PeriodEnd = "periodEnd";
    public const string PeriodStart = "periodStart";
    public const string Days = "days";
    public const string Count = "count";
}
