// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Renders the HTML login and consent page for the OAuth authorize endpoint as a plain
/// content result, since the project has no Razor page infrastructure. All dynamic values
/// are HTML-encoded and the OAuth request parameters travel as hidden form fields together
/// with the antiforgery request token.
/// </summary>

using System.Text;
using System.Text.Encodings.Web;
using Klacks.Api.Domain.Constants;

namespace Klacks.Api.Presentation.Controllers.OAuth;

public static class OAuthLoginPageRenderer
{
    private const string PageTemplate = """
        <!DOCTYPE html>
        <html lang="en">
        <head>
        <meta charset="utf-8">
        <meta name="viewport" content="width=device-width, initial-scale=1">
        <title>Sign in to Klacks</title>
        <style>
        body { font-family: system-ui, -apple-system, "Segoe UI", sans-serif; background: #f4f5f7; margin: 0; display: flex; justify-content: center; align-items: center; min-height: 100vh; }
        .card { background: #fff; border-radius: 12px; box-shadow: 0 4px 16px rgba(0,0,0,.08); padding: 2rem; width: 100%; max-width: 24rem; }
        h1 { font-size: 1.25rem; margin: 0 0 .5rem; }
        p.consent { color: #555; font-size: .9rem; margin: 0 0 1.25rem; }
        label { display: block; font-size: .85rem; margin-bottom: .25rem; color: #333; }
        input[type=email], input[type=password] { width: 100%; box-sizing: border-box; padding: .55rem .7rem; margin-bottom: 1rem; border: 1px solid #cdd2d9; border-radius: 8px; font-size: .95rem; }
        .error { background: #fdecea; color: #b3261e; border-radius: 8px; padding: .6rem .8rem; font-size: .85rem; margin-bottom: 1rem; }
        .actions { display: flex; gap: .75rem; }
        button { flex: 1; padding: .6rem; border: none; border-radius: 8px; font-size: .95rem; cursor: pointer; }
        button.approve { background: #2563eb; color: #fff; }
        button.deny { background: #e5e7eb; color: #333; }
        </style>
        </head>
        <body>
        <main class="card">
        <h1>Sign in to Klacks</h1>
        <p class="consent"><strong>{{CLIENT_NAME}}</strong> is requesting access to your Klacks account.</p>
        {{ERROR_BLOCK}}
        <form method="post" action="{{FORM_ACTION}}">
        {{HIDDEN_FIELDS}}
        <label for="email">Email</label>
        <input type="email" id="email" name="{{EMAIL_FIELD}}" required autofocus autocomplete="username">
        <label for="password">Password</label>
        <input type="password" id="password" name="{{PASSWORD_FIELD}}" required autocomplete="current-password">
        <div class="actions">
        <button class="deny" type="submit" name="{{DECISION_FIELD}}" value="{{DECISION_DENY}}" formnovalidate>Deny</button>
        <button class="approve" type="submit" name="{{DECISION_FIELD}}" value="{{DECISION_APPROVE}}">Allow access</button>
        </div>
        </form>
        </main>
        </body>
        </html>
        """;

    private const string ErrorTemplate = """<div class="error">{{MESSAGE}}</div>""";

    private const string HiddenFieldTemplate = """<input type="hidden" name="{{NAME}}" value="{{VALUE}}">""";

    public static string Render(
        string formAction,
        string clientName,
        IReadOnlyDictionary<string, string> hiddenFields,
        string? errorMessage)
    {
        var encoder = HtmlEncoder.Default;

        var hiddenFieldsBuilder = new StringBuilder();
        foreach (var field in hiddenFields)
        {
            if (string.IsNullOrEmpty(field.Value))
            {
                continue;
            }

            hiddenFieldsBuilder.AppendLine(HiddenFieldTemplate
                .Replace("{{NAME}}", encoder.Encode(field.Key))
                .Replace("{{VALUE}}", encoder.Encode(field.Value)));
        }

        var errorBlock = string.IsNullOrEmpty(errorMessage)
            ? string.Empty
            : ErrorTemplate.Replace("{{MESSAGE}}", encoder.Encode(errorMessage));

        return PageTemplate
            .Replace("{{CLIENT_NAME}}", encoder.Encode(clientName))
            .Replace("{{ERROR_BLOCK}}", errorBlock)
            .Replace("{{FORM_ACTION}}", encoder.Encode(formAction))
            .Replace("{{HIDDEN_FIELDS}}", hiddenFieldsBuilder.ToString())
            .Replace("{{EMAIL_FIELD}}", OAuthConstants.ParameterEmail)
            .Replace("{{PASSWORD_FIELD}}", OAuthConstants.ParameterPassword)
            .Replace("{{DECISION_FIELD}}", OAuthConstants.ParameterDecision)
            .Replace("{{DECISION_DENY}}", OAuthConstants.DecisionDeny)
            .Replace("{{DECISION_APPROVE}}", OAuthConstants.DecisionApprove);
    }
}
