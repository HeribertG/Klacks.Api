// Copyright (c) Heribert Gasparoli Private. All rights reserved.

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
