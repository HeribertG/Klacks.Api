// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

using System.Text;

namespace Klacks.Api.Infrastructure.Services.Identity;

/// <summary>
/// Escapes a value for use inside an LDAP search filter (RFC 4515): backslash, asterisk, parentheses and NUL become
/// their \hh form, so typed text cannot change the structure of the filter.
/// </summary>
public static class LdapFilterEncoder
{
    private const char Backslash = '\\';
    private const char Asterisk = '*';
    private const char OpenParenthesis = '(';
    private const char CloseParenthesis = ')';
    private const char Nul = '\0';
    private const string EscapedBackslash = "\\5c";
    private const string EscapedAsterisk = "\\2a";
    private const string EscapedOpenParenthesis = "\\28";
    private const string EscapedCloseParenthesis = "\\29";
    private const string EscapedNul = "\\00";

    /// <summary>
    /// Returns the value with every filter metacharacter escaped.
    /// </summary>
    /// <param name="value">Raw value, e.g. a typed login name</param>
    public static string Escape(string value)
    {
        var builder = new StringBuilder(value.Length);
        foreach (var character in value)
        {
            switch (character)
            {
                case Backslash:
                    builder.Append(EscapedBackslash);
                    break;
                case Asterisk:
                    builder.Append(EscapedAsterisk);
                    break;
                case OpenParenthesis:
                    builder.Append(EscapedOpenParenthesis);
                    break;
                case CloseParenthesis:
                    builder.Append(EscapedCloseParenthesis);
                    break;
                case Nul:
                    builder.Append(EscapedNul);
                    break;
                default:
                    builder.Append(character);
                    break;
            }
        }

        return builder.ToString();
    }
}
