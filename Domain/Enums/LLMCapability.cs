// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

namespace Klacks.Api.Domain.Enums;

public enum LLMCapability
{
    Chat,
    FunctionCalling,
    Vision,
    ImageGeneration,
    CodeGeneration,
    Embedding,
    TextToSpeech,
    SpeechToText
}