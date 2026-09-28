// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

namespace Klacks.Api.Domain.Enums;

public enum LLMFunctionResultKind
{
    None = 0,
    Data,
    MessageOnly,
    Error,
    Confirmation,
    UiPassthrough,
    FrontendOnly
}
