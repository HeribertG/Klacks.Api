// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Reads the OUTPUT channels of a macro script with the script tokenizer, without executing it. The channel
/// of an OUTPUT is an arbitrary expression evaluated only at runtime, so a probe run would see just the
/// branches its inputs reach; the token stream covers every statement. An OUTPUT counts as literal when its
/// channel is a whole number immediately followed by the comma. The tokenizer runs inside a hard wall-clock
/// budget, and a tokenizer error or exception fails the scan instead of passing it. A tokenizer exception is
/// reported with a fixed, actionable message, never with the raw .NET text: the only known crash is a comment on
/// the last line without a line break after it (the tokenizer then reads before the start of the text).
/// </summary>
/// <param name="content">The macro script (or appended script block) to scan</param>

using System.Globalization;
using Klacks.Api.Domain.Interfaces.Macros;
using Klacks.Api.Domain.Models.Macros;
using Klacks.Api.Infrastructure.Scripting;

namespace Klacks.Api.Infrastructure.Services.Macros;

public class MacroOutputChannelInspector : IMacroOutputChannelInspector
{
    private const int InspectionTimeoutMs = 5000;
    private const string TimeoutMessage = "the script could not be tokenized within {0} ms.";
    private const string TokenizerErrorMessage = "line {0}: {1}";
    private const string TrailingCommentMessage =
        "there is a comment on the last line of the script without a line break after it, which the script parser "
        + "cannot handle; remove that comment or move it to an earlier line";
    private const string TokenizerCrashMessage =
        "the script tokenizer stopped unexpectedly; check the script for unusual characters and simplify it";

    public MacroOutputChannelScan Inspect(string content)
    {
        var scan = Task.Run(() => Scan(content));
        try
        {
            if (!scan.Wait(InspectionTimeoutMs))
            {
                return MacroOutputChannelScan.Failure(
                    string.Format(CultureInfo.InvariantCulture, TimeoutMessage, InspectionTimeoutMs));
            }

            return scan.Result;
        }
        catch (AggregateException ex)
        {
            return MacroOutputChannelScan.Failure(
                ex.InnerException is ArgumentOutOfRangeException ? TrailingCommentMessage : TokenizerCrashMessage);
        }
    }

    private static MacroOutputChannelScan Scan(string content)
    {
        var errors = new InterpreterError();
        var lexer = new LexicalAnalyser().Connect(new StringInputStream().Connect(content), errors);
        var literalChannels = new List<int>();
        var nonLiteralCount = 0;

        var symbol = lexer.GetNextSymbol();
        while (symbol.Token != Symbol.Tokens.tokEof && errors.Number == 0)
        {
            if (symbol.Token != Symbol.Tokens.tokOutput)
            {
                symbol = lexer.GetNextSymbol();
                continue;
            }

            var channel = lexer.GetNextSymbol();
            if (channel.Token == Symbol.Tokens.tokLeftParent)
            {
                channel = lexer.GetNextSymbol();
            }

            symbol = lexer.GetNextSymbol();
            if (TryReadLiteralChannel(channel, symbol, out var literal))
            {
                literalChannels.Add(literal);
            }
            else
            {
                nonLiteralCount++;
            }
        }

        return errors.Number != 0
            ? MacroOutputChannelScan.Failure(
                string.Format(CultureInfo.InvariantCulture, TokenizerErrorMessage, errors.Line, errors.Description))
            : new MacroOutputChannelScan(literalChannels, nonLiteralCount, null);
    }

    private static bool TryReadLiteralChannel(Symbol channel, Symbol following, out int value)
    {
        value = 0;
        if (channel.Token != Symbol.Tokens.tokNumber
            || following.Token != Symbol.Tokens.tokComma
            || channel.Value is not double number
            || number != Math.Floor(number)
            || number > int.MaxValue)
        {
            return false;
        }

        value = (int)number;
        return true;
    }
}
