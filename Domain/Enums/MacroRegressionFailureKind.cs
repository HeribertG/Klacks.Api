// Copyright (c) Heribert Gasparoli Private. All rights reserved.

namespace Klacks.Api.Domain.Enums;

public enum MacroRegressionFailureKind
{
    None = 0,
    OriginalCompileError = 1,
    CopyCompileError = 2,
    CopyRuntimeError = 3,
    NoComparableSample = 4,
    BudgetExceeded = 5
}
