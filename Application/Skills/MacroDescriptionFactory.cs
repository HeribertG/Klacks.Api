// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Builds the multi-language description of a macro created by an assistant skill or by the company-rule apply
/// flow (ApplyCompanyRuleCommandHandler): the one given text is
/// applied to every core language, an empty text yields an empty description.
/// </summary>
/// <param name="description">The description text supplied by the model, or null</param>

using Klacks.Api.Domain.Common;

namespace Klacks.Api.Application.Skills;

internal static class MacroDescriptionFactory
{
    public static MultiLanguage ForAllCoreLanguages(string? description)
    {
        var value = new MultiLanguage();
        if (string.IsNullOrWhiteSpace(description))
        {
            return value;
        }

        foreach (var language in MultiLanguage.CoreLanguages)
        {
            value.SetValue(language, description);
        }

        return value;
    }
}
