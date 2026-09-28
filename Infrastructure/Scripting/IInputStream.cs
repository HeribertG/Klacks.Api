// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

namespace Klacks.Api.Infrastructure.Scripting
{
    public interface IInputStream
    {
        int Col { get; }

        bool Eof { get; }

        InterpreterError ErrorObject { get; set; }

        int Index { get; }

        int Line { get; }

        IInputStream Connect(string connectString);

        string GetNextChar();

        void GoBack();

        void SkipComment();
    }
}
