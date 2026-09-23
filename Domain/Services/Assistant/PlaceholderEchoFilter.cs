// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Holds back the content tokens of one streaming provider call while they could still be nothing but an
/// echo of the tool-call stand-in text (AnswerPlaceholder). The chat client keeps streamed text as the
/// final message, so an echo that reached it could not be retracted. Tokens are released as soon as the
/// content diverges from every stand-in form, minus any complete stand-in it started with; content that
/// stays a pure echo until the call ends is dropped. Ordinary answers are delayed by at most the few
/// characters that happen to match the beginning of a stand-in.
/// </summary>
using System.Text;

namespace Klacks.Api.Domain.Services.Assistant;

internal sealed class PlaceholderEchoFilter
{
    private readonly StringBuilder _held = new();
    private bool _released;

    /// <summary>
    /// Returns the text that may be shown now, empty while the content is still held back.
    /// </summary>
    /// <param name="token">The next content token of the provider call.</param>
    internal string Push(string token)
    {
        if (_released)
        {
            return token;
        }

        _held.Append(token);
        var held = _held.ToString();
        if (AnswerPlaceholder.CouldBecomePlaceholder(held))
        {
            return string.Empty;
        }

        _released = true;
        _held.Clear();
        return AnswerPlaceholder.Visible(held);
    }

    /// <summary>
    /// Returns what is still held back once the call ended, empty when it was nothing but an echo.
    /// </summary>
    internal string Flush()
    {
        if (_released)
        {
            return string.Empty;
        }

        var held = _held.ToString();
        _held.Clear();
        _released = true;
        return AnswerPlaceholder.Visible(held);
    }
}
