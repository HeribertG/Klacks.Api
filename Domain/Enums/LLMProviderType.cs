// Copyright (c) Heribert Gasparoli Private. All rights reserved.

namespace Klacks.Api.Domain.Enums;

// Values are persisted as integers (skill_usage_records.provider_id) — keep them pinned.
// Cohere (10) was removed 2026-07-08; its value must not be reused.
public enum LLMProviderType
{
    OpenAI = 0,
    Anthropic = 1,
    Google = 2,
    Azure = 3,
    Local = 4,
    Baidu = 5,
    Alibaba = 6,
    Tencent = 7,
    ByteDance = 8,
    Mistral = 9,
    HuggingFace = 11,
    DeepSeek = 12,
    Apertus = 13
}
