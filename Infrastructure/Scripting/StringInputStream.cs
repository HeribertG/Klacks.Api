namespace Klacks.Api.Infrastructure.Scripting
{
    public class StringInputStream : IInputStream
    {
        private string? sourcetext;

        public int Col { get; private set; }
        public int Index { get; private set; }
        public int Line { get; private set; }
        public bool Eof => Index >= (sourcetext?.Length ?? 0);
        public InterpreterError ErrorObject { get; set; } = new();

        public IInputStream Connect(string sourcetext)
        {
            this.sourcetext = sourcetext + " ";
            Index = 0;
            Line = 1;
            Col = 0;
            ErrorObject = new InterpreterError();
            return this;
        }

        public string GetNextChar()
        {
            Index++;

            if (Index >= sourcetext!.Length)
            {
                return string.Empty;
            }

            string nextChar = sourcetext.Substring(Index - 1, 1);
            Col++;

            return nextChar switch
            {
                "\t" => " ",
                "\r" => HandleCarriageReturn(),
                "\n" => HandleNewLine(),
                _ => HandleDefaultChar(nextChar)
            };
        }

        private string HandleCarriageReturn()
        {
            var tmp = sourcetext!.Substring(Index, 1);
            if (tmp == "\n")
            {
                Index++;
                Line++;
                Col = 0;
                return "\n";
            }

            Col--;
            return "\r";
        }

        private string HandleNewLine()
        {
            var tmp = sourcetext!.Substring(Index, 1);
            if (tmp == "\r")
            {
                Index++;
            }

            Line++;
            Col = 0;
            return "\n";
        }

        private string HandleDefaultChar(string nextChar)
        {
            if (nextChar.Length >= 1)
            {
                return nextChar;
            }

            ErrorObject.Raise(
                (int)InterpreterError.InputStreamErrors.errInvalidChar,
                "StringInputStream.GetNextChar",
                "Invalid character (ASCII " + nextChar.Substring(0, 1) + ")",
                Line, Col, Index);
            return string.Empty;
        }

        public void GoBack()
        {
            if (Eof || Index <= 0)
            {
                if (Index <= 0)
                {
                    ErrorObject.Raise(
                        (int)InterpreterError.InputStreamErrors.errGoBackPastStartOfSource,
                        "StringInputStream.GoBack",
                        "GoBack past start of source",
                        0, 0, 0);
                }
                return;
            }

            Col--;
            string c = sourcetext?.Substring(Index, 1) ?? string.Empty;

            if (c is "\r\n" or "\n" or "\r")
            {
                Line--;
            }

            Index--;
        }

        public void SkipComment()
        {
            if (sourcetext == null)
            {
                return;
            }

            int i = sourcetext.IndexOf("\n", Index);
            if (i == -1)
                i = sourcetext.IndexOf("\r", Index);
            if (i == -1)
                i = sourcetext.IndexOf("\r\n", Index);
            if (i == 0)
                i = sourcetext.Length + 1;

            Col += i - Index;
            Index = i;
        }
    }
}
