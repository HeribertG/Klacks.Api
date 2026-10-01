// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

namespace Klacks.Api.Domain.Exceptions;

public class DatabaseUpdateException : Exception
{
    public bool IsDuplicate { get; }
    public bool IsForeignKeyViolation { get; }

    public string? ConstraintName { get; }

    public DatabaseUpdateException(string message, Exception? innerException = null,
        bool isDuplicate = false, bool isForeignKeyViolation = false, string? constraintName = null)
        : base(message, innerException)
    {
        IsDuplicate = isDuplicate;
        IsForeignKeyViolation = isForeignKeyViolation;
        ConstraintName = constraintName;
    }
}
