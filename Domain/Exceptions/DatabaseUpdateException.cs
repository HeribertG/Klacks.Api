// Copyright (c) Heribert Gasparoli Private. All rights reserved.

namespace Klacks.Api.Domain.Exceptions;

public class DatabaseUpdateException : Exception
{
    public bool IsDuplicate { get; }
    public bool IsForeignKeyViolation { get; }

    public DatabaseUpdateException(string message, Exception? innerException = null,
        bool isDuplicate = false, bool isForeignKeyViolation = false)
        : base(message, innerException)
    {
        IsDuplicate = isDuplicate;
        IsForeignKeyViolation = isForeignKeyViolation;
    }
}
