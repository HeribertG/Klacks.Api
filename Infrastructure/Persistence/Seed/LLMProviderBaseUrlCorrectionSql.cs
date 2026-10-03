// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Correction of the seeded Qwen (Alibaba Model Studio) base URL. Older builds seeded the native DashScope API
/// (https://dashscope.aliyuncs.com/api/v1/), which has no chat/completions route, while every Qwen request goes
/// through the OpenAI-compatible provider - so the provider could never answer. The current seed points at the
/// OpenAI-compatible endpoint of the international (Singapore) region. LLMSeed inserts once, so existing databases
/// keep the old value; the BackfillSeededQwenCompatibleBaseUrl migration applies the correction, and only while the
/// row still holds exactly the old seeded value, so an administrator's own URL survives. Both values are frozen here
/// on purpose: a migration is history.
/// </summary>
namespace Klacks.Api.Data.Seed
{
    public static class LLMProviderBaseUrlCorrectionSql
    {
        public const string ProviderTable = "llm_providers";

        public const string QwenProviderId = "qwen";

        public const string FaultyQwenBaseUrl = "https://dashscope.aliyuncs.com/api/v1/";

        public const string CorrectedQwenBaseUrl = "https://dashscope-intl.aliyuncs.com/compatible-mode/v1/";

        public static string BuildQwenStatement() =>
            $"UPDATE {ProviderTable} SET base_url = '{CorrectedQwenBaseUrl}', update_time = now() "
            + $"WHERE provider_id = '{QwenProviderId}' AND base_url = '{FaultyQwenBaseUrl}';";
    }
}
