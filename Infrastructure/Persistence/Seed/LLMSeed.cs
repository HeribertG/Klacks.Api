// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Microsoft.EntityFrameworkCore.Migrations;

namespace Klacks.Api.Data.Seed;

/// <summary>
/// Seeds the initial LLM provider catalog, including keyless local providers such as Ollama and LM Studio.
/// </summary>
public static class LLMSeed
{
    private const string OllamaProviderGuid = "8f4c1d6a-9b2e-4e7f-a3c5-1d0b7e9f2a41";
    private const string LmStudioProviderGuid = "5b7e3f9c-2d4a-4c8b-9e6f-7a1c3d5b8e02";
    private const string CerebrasProviderGuid = "3d9a5c7e-4f1b-4a6d-8c2e-9b0f6a3d7c15";
    private const string OpenRouterProviderGuid = "7c2f8e4b-6a1d-4f9c-b5e3-2d8a0c6f4b27";

    public static void SeedData(MigrationBuilder migrationBuilder)
    {
        var now = DateTime.UtcNow;

        migrationBuilder.Sql($@"
            INSERT INTO llm_providers (id, provider_id, provider_name, is_enabled, priority, base_url, api_version, requires_api_key, settings, create_time, update_time, is_deleted) VALUES
            (gen_random_uuid(), 'openai', 'OpenAI', true, 1, 'https://api.openai.com/v1/', 'v1', true, NULL, '{now:yyyy-MM-dd HH:mm:ss}', '{now:yyyy-MM-dd HH:mm:ss}', false),
            (gen_random_uuid(), 'anthropic', 'Anthropic', true, 2, 'https://api.anthropic.com/v1/', '2023-06-01', true, NULL, '{now:yyyy-MM-dd HH:mm:ss}', '{now:yyyy-MM-dd HH:mm:ss}', false),
            (gen_random_uuid(), 'google', 'Google Gemini', true, 3, 'https://generativelanguage.googleapis.com/v1/', 'v1', true, NULL, '{now:yyyy-MM-dd HH:mm:ss}', '{now:yyyy-MM-dd HH:mm:ss}', false),
            (gen_random_uuid(), 'azure', 'Azure OpenAI', false, 4, 'https://your-resource.openai.azure.com/', '2023-12-01-preview', true, NULL, '{now:yyyy-MM-dd HH:mm:ss}', '{now:yyyy-MM-dd HH:mm:ss}', false),
            (gen_random_uuid(), 'mistral', 'Mistral AI', false, 5, 'https://api.mistral.ai/v1/', 'v1', true, NULL, '{now:yyyy-MM-dd HH:mm:ss}', '{now:yyyy-MM-dd HH:mm:ss}', false),
            (gen_random_uuid(), 'deepseek', 'DeepSeek', true, 7, 'https://api.deepseek.com/v1/', 'v1', true, NULL, '{now:yyyy-MM-dd HH:mm:ss}', '{now:yyyy-MM-dd HH:mm:ss}', false),
            (gen_random_uuid(), 'qwen', 'Qwen (Alibaba)', false, 8, 'https://dashscope.aliyuncs.com/api/v1/', 'v1', true, NULL, '{now:yyyy-MM-dd HH:mm:ss}', '{now:yyyy-MM-dd HH:mm:ss}', false),
            (gen_random_uuid(), 'baidu', 'Baidu Ernie', false, 9, 'https://aip.baidubce.com/rpc/2.0/', 'v1', true, NULL, '{now:yyyy-MM-dd HH:mm:ss}', '{now:yyyy-MM-dd HH:mm:ss}', false),
            (gen_random_uuid(), 'zhipu', 'Zhipu AI (GLM)', false, 10, 'https://open.bigmodel.cn/api/paas/v4/', 'v4', true, NULL, '{now:yyyy-MM-dd HH:mm:ss}', '{now:yyyy-MM-dd HH:mm:ss}', false),
            (gen_random_uuid(), 'apertus', 'Apertus (Swiss AI)', false, 11, 'https://api.apertus.ai/v1/', 'v1', true, NULL, '{now:yyyy-MM-dd HH:mm:ss}', '{now:yyyy-MM-dd HH:mm:ss}', false),
            (gen_random_uuid(), 'groq', 'Groq', false, 12, 'https://api.groq.com/openai/v1/', 'v1', true, NULL, '{now:yyyy-MM-dd HH:mm:ss}', '{now:yyyy-MM-dd HH:mm:ss}', false),
            (gen_random_uuid(), 'together', 'Together AI', false, 13, 'https://api.together.xyz/v1/', 'v1', true, NULL, '{now:yyyy-MM-dd HH:mm:ss}', '{now:yyyy-MM-dd HH:mm:ss}', false),
            (gen_random_uuid(), 'fireworks', 'Fireworks AI', false, 14, 'https://api.fireworks.ai/inference/v1/', 'v1', true, NULL, '{now:yyyy-MM-dd HH:mm:ss}', '{now:yyyy-MM-dd HH:mm:ss}', false),
            (gen_random_uuid(), 'kimi', 'Kimi (Moonshot AI)', false, 15, 'https://api.kimi.com/coding/v1/', 'v1', true, NULL, '{now:yyyy-MM-dd HH:mm:ss}', '{now:yyyy-MM-dd HH:mm:ss}', false);
        ");

        migrationBuilder.Sql($@"
            INSERT INTO llm_providers (id, provider_id, provider_name, is_enabled, priority, base_url, api_version, requires_api_key, settings, create_time, update_time, is_deleted)
            SELECT '{OllamaProviderGuid}', 'ollama', 'Ollama (local)', false, 16, 'http://localhost:11434/v1/', 'v1', false, NULL, '{now:yyyy-MM-dd HH:mm:ss}', '{now:yyyy-MM-dd HH:mm:ss}', false
            WHERE NOT EXISTS (SELECT 1 FROM llm_providers WHERE provider_id = 'ollama');
        ");

        migrationBuilder.Sql($@"
            INSERT INTO llm_providers (id, provider_id, provider_name, is_enabled, priority, base_url, api_version, requires_api_key, settings, create_time, update_time, is_deleted)
            SELECT '{LmStudioProviderGuid}', 'lm-studio', 'LM Studio (local)', false, 17, 'http://localhost:1234/v1/', 'v1', false, NULL, '{now:yyyy-MM-dd HH:mm:ss}', '{now:yyyy-MM-dd HH:mm:ss}', false
            WHERE NOT EXISTS (SELECT 1 FROM llm_providers WHERE provider_id = 'lm-studio');
        ");

        migrationBuilder.Sql($@"
            INSERT INTO llm_providers (id, provider_id, provider_name, is_enabled, priority, base_url, api_version, requires_api_key, settings, create_time, update_time, is_deleted)
            SELECT '{CerebrasProviderGuid}', 'cerebras', 'Cerebras', false, 18, 'https://api.cerebras.ai/v1/', 'v1', true, NULL, '{now:yyyy-MM-dd HH:mm:ss}', '{now:yyyy-MM-dd HH:mm:ss}', false
            WHERE NOT EXISTS (SELECT 1 FROM llm_providers WHERE provider_id = 'cerebras');
        ");

        migrationBuilder.Sql($@"
            INSERT INTO llm_providers (id, provider_id, provider_name, is_enabled, priority, base_url, api_version, requires_api_key, settings, create_time, update_time, is_deleted)
            SELECT '{OpenRouterProviderGuid}', 'openrouter', 'OpenRouter', false, 19, 'https://openrouter.ai/api/v1/', 'v1', true, NULL, '{now:yyyy-MM-dd HH:mm:ss}', '{now:yyyy-MM-dd HH:mm:ss}', false
            WHERE NOT EXISTS (SELECT 1 FROM llm_providers WHERE provider_id = 'openrouter');
        ");

        migrationBuilder.Sql($@"
            INSERT INTO llm_models (id, model_id, model_name, api_model_id, provider_id, is_enabled, is_default, cost_per_input_token, cost_per_output_token, max_tokens, context_window, category, create_time, update_time, is_deleted) VALUES
            (gen_random_uuid(), 'gpt-54', 'GPT-5.4', 'gpt-5.4', 'openai', true, true, 0.0025, 0.015, 128000, 1050000, 'powerful', '{now:yyyy-MM-dd HH:mm:ss}', '{now:yyyy-MM-dd HH:mm:ss}', false),
            (gen_random_uuid(), 'gpt-54-mini', 'GPT-5.4 Mini', 'gpt-5.4-mini', 'openai', true, false, 0.00075, 0.0045, 128000, 400000, 'fast', '{now:yyyy-MM-dd HH:mm:ss}', '{now:yyyy-MM-dd HH:mm:ss}', false),
            (gen_random_uuid(), 'gpt-54-nano', 'GPT-5.4 Nano', 'gpt-5.4-nano', 'openai', true, false, 0.0002, 0.00125, 128000, 400000, 'fast', '{now:yyyy-MM-dd HH:mm:ss}', '{now:yyyy-MM-dd HH:mm:ss}', false),
            (gen_random_uuid(), 'gpt-53-codex', 'GPT-5.3 Codex', 'gpt-5.3-codex', 'openai', true, false, 0.00175, 0.014, 128000, 400000, 'coding', '{now:yyyy-MM-dd HH:mm:ss}', '{now:yyyy-MM-dd HH:mm:ss}', false),
            (gen_random_uuid(), 'claude-opus-48', 'Claude Opus 4.8', 'claude-opus-4-8', 'anthropic', true, false, 0.005, 0.025, 128000, 1000000, 'powerful', '{now:yyyy-MM-dd HH:mm:ss}', '{now:yyyy-MM-dd HH:mm:ss}', false),
            (gen_random_uuid(), 'claude-sonnet-5', 'Claude Sonnet 5', 'claude-sonnet-5', 'anthropic', true, false, 0.003, 0.015, 128000, 1000000, 'balanced', '{now:yyyy-MM-dd HH:mm:ss}', '{now:yyyy-MM-dd HH:mm:ss}', false),
            (gen_random_uuid(), 'claude-fable-5', 'Claude Fable 5', 'claude-fable-5', 'anthropic', true, false, 0.010, 0.050, 128000, 1000000, 'powerful', '{now:yyyy-MM-dd HH:mm:ss}', '{now:yyyy-MM-dd HH:mm:ss}', false),
            (gen_random_uuid(), 'claude-haiku-45', 'Claude Haiku 4.5', 'claude-haiku-4-5-20251001', 'anthropic', false, false, 0.001, 0.005, 64000, 200000, 'fast', '{now:yyyy-MM-dd HH:mm:ss}', '{now:yyyy-MM-dd HH:mm:ss}', false),
            (gen_random_uuid(), 'gemini-31-pro', 'Gemini 3.1 Pro', 'gemini-3.1-pro-preview', 'google', true, true, 0.002, 0.012, 64000, 2000000, 'powerful', '{now:yyyy-MM-dd HH:mm:ss}', '{now:yyyy-MM-dd HH:mm:ss}', false),
            (gen_random_uuid(), 'gemini-3-flash', 'Gemini 3 Flash', 'gemini-3-flash-preview', 'google', true, false, 0.0005, 0.003, 64000, 1000000, 'balanced', '{now:yyyy-MM-dd HH:mm:ss}', '{now:yyyy-MM-dd HH:mm:ss}', false),
            (gen_random_uuid(), 'gemini-31-flash-lite', 'Gemini 3.1 Flash Lite', 'gemini-3.1-flash-lite-preview', 'google', true, false, 0.0001, 0.0004, 8192, 1000000, 'fast', '{now:yyyy-MM-dd HH:mm:ss}', '{now:yyyy-MM-dd HH:mm:ss}', false),
            (gen_random_uuid(), 'gemini-25-flash', 'Gemini 2.5 Flash', 'gemini-2.5-flash', 'google', true, false, 0.0003, 0.0025, 8192, 1000000, 'fast', '{now:yyyy-MM-dd HH:mm:ss}', '{now:yyyy-MM-dd HH:mm:ss}', false),
            (gen_random_uuid(), 'mistral-large-3', 'Mistral Large 3', 'mistral-large-2512', 'mistral', false, false, 0.0005, 0.0015, 8192, 262000, 'powerful', '{now:yyyy-MM-dd HH:mm:ss}', '{now:yyyy-MM-dd HH:mm:ss}', false),
            (gen_random_uuid(), 'mistral-small-4', 'Mistral Small 4', 'mistral-small-2603', 'mistral', false, false, 0.00015, 0.00045, 8192, 262000, 'fast', '{now:yyyy-MM-dd HH:mm:ss}', '{now:yyyy-MM-dd HH:mm:ss}', false),
            (gen_random_uuid(), 'magistral-medium', 'Magistral Medium', 'magistral-medium-2509', 'mistral', false, false, 0.002, 0.005, 8192, 40000, 'reasoning', '{now:yyyy-MM-dd HH:mm:ss}', '{now:yyyy-MM-dd HH:mm:ss}', false),
            (gen_random_uuid(), 'devstral-2', 'Devstral 2', 'devstral-2512', 'mistral', false, false, 0.0004, 0.0009, 16384, 262000, 'coding', '{now:yyyy-MM-dd HH:mm:ss}', '{now:yyyy-MM-dd HH:mm:ss}', false),
            (gen_random_uuid(), 'deepseek-chat', 'DeepSeek V3.2 Chat', 'deepseek-chat', 'deepseek', true, false, 0.00028, 0.00042, 8192, 128000, 'balanced', '{now:yyyy-MM-dd HH:mm:ss}', '{now:yyyy-MM-dd HH:mm:ss}', false),
            (gen_random_uuid(), 'deepseek-reasoner', 'DeepSeek V3.2 Reasoner', 'deepseek-reasoner', 'deepseek', true, false, 0.00028, 0.00042, 64000, 128000, 'reasoning', '{now:yyyy-MM-dd HH:mm:ss}', '{now:yyyy-MM-dd HH:mm:ss}', false),
            (gen_random_uuid(), 'qwen3-max', 'Qwen3 Max', 'qwen3-max', 'qwen', false, false, 0.002, 0.006, 8192, 128000, 'powerful', '{now:yyyy-MM-dd HH:mm:ss}', '{now:yyyy-MM-dd HH:mm:ss}', false),
            (gen_random_uuid(), 'qwen3-32b', 'Qwen3 32B', 'qwen3-32b', 'qwen', false, false, 0.0008, 0.002, 8192, 128000, 'balanced', '{now:yyyy-MM-dd HH:mm:ss}', '{now:yyyy-MM-dd HH:mm:ss}', false),
            (gen_random_uuid(), 'ernie-5', 'ERNIE 5.0', 'ernie-5.0', 'baidu', false, false, 0.002, 0.006, 8192, 128000, 'powerful', '{now:yyyy-MM-dd HH:mm:ss}', '{now:yyyy-MM-dd HH:mm:ss}', false),
            (gen_random_uuid(), 'ernie-45-vl', 'ERNIE 4.5 VL', 'ernie-4.5-vl', 'baidu', false, false, 0.0015, 0.004, 8192, 128000, 'balanced', '{now:yyyy-MM-dd HH:mm:ss}', '{now:yyyy-MM-dd HH:mm:ss}', false),
            (gen_random_uuid(), 'glm-46', 'GLM-4.6', 'glm-4.6', 'zhipu', false, false, 0.002, 0.006, 8192, 200000, 'powerful', '{now:yyyy-MM-dd HH:mm:ss}', '{now:yyyy-MM-dd HH:mm:ss}', false),
            (gen_random_uuid(), 'glm-45', 'GLM-4.5', 'glm-4.5', 'zhipu', false, false, 0.001, 0.003, 8192, 128000, 'balanced', '{now:yyyy-MM-dd HH:mm:ss}', '{now:yyyy-MM-dd HH:mm:ss}', false),
            (gen_random_uuid(), 'apertus-70b', 'Apertus 70B Instruct', 'swiss-ai/Apertus-70B-Instruct-2509', 'apertus', false, false, 0.0009, 0.0009, 8192, 65536, 'powerful', '{now:yyyy-MM-dd HH:mm:ss}', '{now:yyyy-MM-dd HH:mm:ss}', false),
            (gen_random_uuid(), 'apertus-8b', 'Apertus 8B Instruct', 'swiss-ai/Apertus-8B-Instruct-2509', 'apertus', false, false, 0.0002, 0.0002, 8192, 65536, 'fast', '{now:yyyy-MM-dd HH:mm:ss}', '{now:yyyy-MM-dd HH:mm:ss}', false),
            (gen_random_uuid(), 'llama-4-maverick', 'Llama 4 Maverick', 'meta-llama/Llama-4-Maverick-17B-128E-Instruct', 'groq', false, false, 0.0002, 0.0006, 8192, 131072, 'fast', '{now:yyyy-MM-dd HH:mm:ss}', '{now:yyyy-MM-dd HH:mm:ss}', false),
            (gen_random_uuid(), 'llama-4-scout', 'Llama 4 Scout', 'meta-llama/Llama-4-Scout-17B-16E-Instruct', 'groq', false, false, 0.00011, 0.00034, 8192, 131072, 'balanced', '{now:yyyy-MM-dd HH:mm:ss}', '{now:yyyy-MM-dd HH:mm:ss}', false),
            (gen_random_uuid(), 'qwen3-235b-together', 'Qwen3 235B (Together)', 'Qwen/Qwen3-235B-A22B', 'together', false, false, 0.0012, 0.0012, 8192, 131072, 'powerful', '{now:yyyy-MM-dd HH:mm:ss}', '{now:yyyy-MM-dd HH:mm:ss}', false),
            (gen_random_uuid(), 'deepseek-v3-fireworks', 'DeepSeek V3 (Fireworks)', 'accounts/fireworks/models/deepseek-v3', 'fireworks', false, false, 0.0009, 0.0009, 8192, 131072, 'balanced', '{now:yyyy-MM-dd HH:mm:ss}', '{now:yyyy-MM-dd HH:mm:ss}', false),
            (gen_random_uuid(), 'kimi-for-coding', 'Kimi K2.5 for Coding', 'kimi-for-coding', 'kimi', false, false, 0.00045, 0.0022, 65535, 262144, 'coding', '{now:yyyy-MM-dd HH:mm:ss}', '{now:yyyy-MM-dd HH:mm:ss}', false),
            (gen_random_uuid(), 'gemma2-9b', 'Gemma 2 9B (Groq)', 'gemma2-9b-it', 'groq', false, false, 0.0, 0.0, 8192, 8192, 'fast', '{now:yyyy-MM-dd HH:mm:ss}', '{now:yyyy-MM-dd HH:mm:ss}', false),
            (gen_random_uuid(), 'gemma-4-26b-a4b', 'Gemma 4 26B (MoE)', 'gemma-4-26b-a4b-it', 'google', true, false, 0.0, 0.0, 8192, 128000, 'balanced', '{now:yyyy-MM-dd HH:mm:ss}', '{now:yyyy-MM-dd HH:mm:ss}', false),
            (gen_random_uuid(), 'gemma-4-31b', 'Gemma 4 31B', 'gemma-4-31b-it', 'google', true, false, 0.0, 0.0, 8192, 128000, 'powerful', '{now:yyyy-MM-dd HH:mm:ss}', '{now:yyyy-MM-dd HH:mm:ss}', false);
        ");

        SeedModelIfMissing(migrationBuilder, now, "gpt-oss-120b-cerebras", "GPT-OSS 120B (Cerebras)", "gpt-oss-120b", "cerebras", 0.00035, 0.00075, 32768, 131072, "balanced");
        SeedModelIfMissing(migrationBuilder, now, "gemma-4-31b-cerebras", "Gemma 4 31B (Cerebras)", "gemma-4-31b", "cerebras", 0.001, 0.0015, 8192, 131072, "powerful");
        SeedModelIfMissing(migrationBuilder, now, "qwen3-32b-cerebras", "Qwen3 32B (Cerebras)", "qwen-3-32b", "cerebras", 0.0004, 0.0008, 8192, 131072, "fast");

        SeedModelIfMissing(migrationBuilder, now, "nemotron-3-ultra-openrouter", "Nemotron 3 Ultra 550B (OpenRouter, free)", "nvidia/nemotron-3-ultra-550b-a55b:free", "openrouter", 0.0, 0.0, 8192, 1000000, "powerful");
        SeedModelIfMissing(migrationBuilder, now, "nemotron-3-super-openrouter", "Nemotron 3 Super 120B (OpenRouter, free)", "nvidia/nemotron-3-super-120b-a12b:free", "openrouter", 0.0, 0.0, 8192, 262144, "balanced");
        SeedModelIfMissing(migrationBuilder, now, "gemma-4-31b-openrouter", "Gemma 4 31B (OpenRouter, free)", "google/gemma-4-31b-it:free", "openrouter", 0.0, 0.0, 8192, 262144, "powerful");
        SeedModelIfMissing(migrationBuilder, now, "gpt-oss-20b-openrouter", "GPT-OSS 20B (OpenRouter, free)", "openai/gpt-oss-20b:free", "openrouter", 0.0, 0.0, 8192, 131072, "fast");
    }

    private static void SeedModelIfMissing(
        MigrationBuilder migrationBuilder,
        DateTime now,
        string modelId,
        string modelName,
        string apiModelId,
        string providerId,
        double costPerInputToken,
        double costPerOutputToken,
        int maxTokens,
        int contextWindow,
        string category)
    {
        migrationBuilder.Sql(FormattableString.Invariant($@"
            INSERT INTO llm_models (id, model_id, model_name, api_model_id, provider_id, is_enabled, is_default, cost_per_input_token, cost_per_output_token, max_tokens, context_window, category, create_time, update_time, is_deleted)
            SELECT gen_random_uuid(), '{modelId}', '{modelName}', '{apiModelId}', '{providerId}', false, false, {costPerInputToken}, {costPerOutputToken}, {maxTokens}, {contextWindow}, '{category}', '{now:yyyy-MM-dd HH:mm:ss}', '{now:yyyy-MM-dd HH:mm:ss}', false
            WHERE NOT EXISTS (SELECT 1 FROM llm_models WHERE model_id = '{modelId}');
        "));
    }
}