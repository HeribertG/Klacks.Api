// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

namespace Klacks.Api.Infrastructure.Scripting;

public record InterpreterErrorInfo(
    int Number,
    string Source,
    string Description,
    int Line,
    int Col,
    int Index,
    string ErrSource = "");
