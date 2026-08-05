// Copyright (c) Heribert Gasparoli Private. All rights reserved.

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
