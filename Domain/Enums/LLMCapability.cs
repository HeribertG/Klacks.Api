// Copyright (c) Heribert Gasparoli Private. All rights reserved.

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