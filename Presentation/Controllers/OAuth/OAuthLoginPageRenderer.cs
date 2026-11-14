// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Renders the HTML consent page for the OAuth authorize endpoint as a plain content result, since the project has no
/// Razor page infrastructure. The tool name, its host and (for a program on this computer) the port come from the
/// trusted-list entry; the sentences come from the OAuthConsentTexts catalogue in the visitor's language. Every dynamic
/// value is HTML-encoded and the OAuth request parameters travel as hidden form fields together with the antiforgery
/// request token (inserted last, so client-controlled values are never scanned for placeholders). The access mode radio is preselected to read; the read-and-change option appears only when the
/// installation allows it. Right-to-left languages get dir="rtl", while host and port stay left to right.
/// </summary>
/// <param name="model">Everything the page shows</param>

using System.Text;
using System.Text.Encodings.Web;
using System.Text.Unicode;
using Klacks.Api.Domain.Constants;
using Klacks.Api.Domain.Enums;
using Klacks.Api.Domain.Models.Authentification;

namespace Klacks.Api.Presentation.Controllers.OAuth;

public static class OAuthLoginPageRenderer
{
    private const string RightToLeft = "rtl";
    private const string LeftToRight = "ltr";

    private const string PageTemplate = """
        <!DOCTYPE html>
        <html lang="{{LANG}}" dir="{{DIR}}">
        <head>
        <meta charset="utf-8">
        <meta name="viewport" content="width=device-width, initial-scale=1">
        <title>{{TITLE}}</title>
        <style>
        body { font-family: system-ui, -apple-system, "Segoe UI", sans-serif; background: #f4f5f7; margin: 0; display: flex; justify-content: center; align-items: center; min-height: 100vh; }
        .card { background: #fff; border-radius: 12px; box-shadow: 0 4px 16px rgba(0,0,0,.08); padding: 2rem; width: 100%; max-width: 28rem; box-sizing: border-box; }
        h1 { font-size: 1.25rem; margin: 0 0 1rem; }
        .tool { background: #eef3ff; border-radius: 10px; padding: .9rem 1rem; margin-bottom: 1rem; }
        .toolhostlabel { color: #555; font-size: .8rem; margin-bottom: .2rem; }
        .toolname { font-size: 1.5rem; font-weight: 700; }
        .toolhost { font-size: 1.15rem; font-weight: 600; word-break: break-all; text-align: start; direction: ltr; unicode-bidi: isolate; }
        p.consent, p.lifetime { color: #555; font-size: .9rem; margin: 0 0 1rem; }
        .warning { background: #fff4e5; color: #7a4b00; border-radius: 8px; padding: .6rem .8rem; font-size: .85rem; margin-bottom: 1rem; }
        fieldset { border: 1px solid #cdd2d9; border-radius: 8px; margin: 0 0 1rem; padding: .6rem .8rem; }
        legend { font-size: .85rem; color: #333; padding: 0 .3rem; }
        label.choice { display: flex; gap: .5rem; align-items: flex-start; font-size: .9rem; margin: .35rem 0; color: #333; }
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
        <h1>{{TITLE}}</h1>
        <div class="tool">
        <div class="toolhostlabel">{{HOST_LABEL}}</div>
        <div class="toolname">{{TOOL_NAME}}</div>
        <div class="toolhost" dir="ltr">{{TOOL_HOST}}</div>
        </div>
        <p class="consent">{{INTRO}}</p>
        {{LOCAL_WARNING}}
        {{ERROR_BLOCK}}
        <form method="post" action="{{FORM_ACTION}}">
        {{HIDDEN_FIELDS}}
        <fieldset>
        <legend>{{ACCESS_LEGEND}}</legend>
        <label class="choice"><input type="radio" name="{{ACCESS_FIELD}}" value="{{ACCESS_READ_VALUE}}" checked><span>{{ACCESS_READ}}</span></label>
        {{WRITE_OPTION}}
        </fieldset>
        <p class="lifetime">{{LIFETIME}}</p>
        <label for="email">{{EMAIL_LABEL}}</label>
        <input type="email" id="email" name="{{EMAIL_FIELD}}" required autofocus autocomplete="username">
        <label for="password">{{PASSWORD_LABEL}}</label>
        <input type="password" id="password" name="{{PASSWORD_FIELD}}" required autocomplete="current-password">
        <div class="actions">
        <button class="deny" type="submit" name="{{DECISION_FIELD}}" value="{{DECISION_DENY}}" formnovalidate>{{DENY}}</button>
        <button class="approve" type="submit" name="{{DECISION_FIELD}}" value="{{DECISION_APPROVE}}">{{ALLOW}}</button>
        </div>
        </form>
        </main>
        </body>
        </html>
        """;

    private const string WarningTemplate = """<div class="warning">{{MESSAGE}}</div>""";

    private const string ErrorTemplate = """<div class="error">{{MESSAGE}}</div>""";

    private const string HiddenFieldTemplate = """<input type="hidden" name="{{NAME}}" value="{{VALUE}}">""";

    private const string WriteOptionTemplate =
        """<label class="choice"><input type="radio" name="{{ACCESS_FIELD}}" value="{{ACCESS_WRITE_VALUE}}"><span>{{ACCESS_WRITE}}</span></label>""";

    private static readonly HtmlEncoder Encoder = HtmlEncoder.Create(UnicodeRanges.All);

    public static string Render(OAuthConsentPageModel model)
    {
        var texts = model.Texts;

        var hiddenFieldsBuilder = new StringBuilder();
        foreach (var field in model.HiddenFields)
        {
            if (string.IsNullOrEmpty(field.Value))
            {
                continue;
            }

            hiddenFieldsBuilder.AppendLine(HiddenFieldTemplate
                .Replace("{{NAME}}", Encoder.Encode(field.Key))
                .Replace("{{VALUE}}", Encoder.Encode(field.Value)));
        }

        var errorBlock = string.IsNullOrEmpty(model.ErrorMessage)
            ? string.Empty
            : ErrorTemplate.Replace("{{MESSAGE}}", Encoder.Encode(model.ErrorMessage));

        var warningBlock = model.Match.Class == OAuthRedirectClass.Loopback
            ? WarningTemplate.Replace(
                "{{MESSAGE}}",
                Encoder.Encode(texts.Get(OAuthConsentTexts.LocalProgramWarning).Replace(OAuthConsentTexts.TargetPlaceholder, FormatHost(model.Match))))
            : string.Empty;

        var writeOption = model.IsWriteAllowed
            ? WriteOptionTemplate
                .Replace("{{ACCESS_FIELD}}", OAuthConstants.ParameterAccessMode)
                .Replace("{{ACCESS_WRITE_VALUE}}", OAuthConstants.AccessModeWrite)
                .Replace("{{ACCESS_WRITE}}", Encoder.Encode(texts.Get(OAuthConsentTexts.AccessWrite)))
            : string.Empty;

        return PageTemplate
            .Replace("{{LANG}}", Encoder.Encode(texts.LanguageCode))
            .Replace("{{DIR}}", texts.IsRightToLeft ? RightToLeft : LeftToRight)
            .Replace("{{TITLE}}", Encoder.Encode(texts.Get(OAuthConsentTexts.Title)))
            .Replace("{{HOST_LABEL}}", Encoder.Encode(texts.Get(OAuthConsentTexts.HostLabel)))
            .Replace("{{TOOL_NAME}}", Encoder.Encode(model.Match.DisplayName))
            .Replace("{{TOOL_HOST}}", Encoder.Encode(FormatHost(model.Match)))
            .Replace("{{INTRO}}", Encoder.Encode(texts.Get(OAuthConsentTexts.Intro)))
            .Replace("{{LOCAL_WARNING}}", warningBlock)
            .Replace("{{ERROR_BLOCK}}", errorBlock)
            .Replace("{{FORM_ACTION}}", Encoder.Encode(model.FormAction))
            .Replace("{{ACCESS_LEGEND}}", Encoder.Encode(texts.Get(OAuthConsentTexts.AccessLegend)))
            .Replace("{{ACCESS_READ}}", Encoder.Encode(texts.Get(OAuthConsentTexts.AccessRead)))
            .Replace("{{WRITE_OPTION}}", writeOption)
            .Replace("{{LIFETIME}}", Encoder.Encode(LifetimeSentence(model)))
            .Replace("{{EMAIL_LABEL}}", Encoder.Encode(texts.Get(OAuthConsentTexts.EmailLabel)))
            .Replace("{{PASSWORD_LABEL}}", Encoder.Encode(texts.Get(OAuthConsentTexts.PasswordLabel)))
            .Replace("{{DENY}}", Encoder.Encode(texts.Get(OAuthConsentTexts.Deny)))
            .Replace("{{ALLOW}}", Encoder.Encode(texts.Get(OAuthConsentTexts.Allow)))
            .Replace("{{ACCESS_FIELD}}", OAuthConstants.ParameterAccessMode)
            .Replace("{{ACCESS_READ_VALUE}}", OAuthConstants.AccessModeRead)
            .Replace("{{EMAIL_FIELD}}", OAuthConstants.ParameterEmail)
            .Replace("{{PASSWORD_FIELD}}", OAuthConstants.ParameterPassword)
            .Replace("{{DECISION_FIELD}}", OAuthConstants.ParameterDecision)
            .Replace("{{DECISION_DENY}}", OAuthConstants.DecisionDeny)
            .Replace("{{DECISION_APPROVE}}", OAuthConstants.DecisionApprove)
            .Replace("{{HIDDEN_FIELDS}}", hiddenFieldsBuilder.ToString());
    }

    private static string LifetimeSentence(OAuthConsentPageModel model)
    {
        var durationKey = model.AccessTokenLifetime >= TimeSpan.FromDays(OAuthConstants.AccessTokenExpiresInDays)
            ? OAuthConsentTexts.DurationDays
            : OAuthConsentTexts.DurationHours;

        return model.Texts.Get(OAuthConsentTexts.Lifetime)
            .Replace(OAuthConsentTexts.DurationPlaceholder, model.Texts.Get(durationKey));
    }

    private static string FormatHost(OAuthRedirectMatch match)
    {
        return match.Port.HasValue ? $"{match.Host}:{match.Port.Value}" : match.Host;
    }
}
