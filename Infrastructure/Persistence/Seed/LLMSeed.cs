// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Microsoft.EntityFrameworkCore.Migrations;

namespace Klacks.Api.Data.Seed;

public static class LLMSeed
{
    public static void SeedData(MigrationBuilder migrationBuilder)
    {
        var now = DateTime.UtcNow;

        migrationBuilder.Sql($@"
            INSERT INTO llm_providers (id, provider_id, provider_name, is_enabled, priority, base_url, api_version, settings, create_time, update_time, is_deleted) VALUES
            (gen_random_uuid(), 'openai', 'OpenAI', true, 1, 'https://api.openai.com/v1/', 'v1', NULL, '{now:yyyy-MM-dd HH:mm:ss}', '{now:yyyy-MM-dd HH:mm:ss}', false),
            (gen_random_uuid(), 'anthropic', 'Anthropic', true, 2, 'https://api.anthropic.com/v1/', '2023-06-01', NULL, '{now:yyyy-MM-dd HH:mm:ss}', '{now:yyyy-MM-dd HH:mm:ss}', false),
            (gen_random_uuid(), 'google', 'Google Gemini', true, 3, 'https://generativelanguage.googleapis.com/v1/', 'v1', NULL, '{now:yyyy-MM-dd HH:mm:ss}', '{now:yyyy-MM-dd HH:mm:ss}', false),
            (gen_random_uuid(), 'azure', 'Azure OpenAI', false, 4, 'https://your-resource.openai.azure.com/', '2023-12-01-preview', NULL, '{now:yyyy-MM-dd HH:mm:ss}', '{now:yyyy-MM-dd HH:mm:ss}', false),
            (gen_random_uuid(), 'mistral', 'Mistral AI', false, 5, 'https://api.mistral.ai/v1/', 'v1', NULL, '{now:yyyy-MM-dd HH:mm:ss}', '{now:yyyy-MM-dd HH:mm:ss}', false),
            (gen_random_uuid(), 'cohere', 'Cohere', false, 6, 'https://api.cohere.ai/v1/', 'v1', NULL, '{now:yyyy-MM-dd HH:mm:ss}', '{now:yyyy-MM-dd HH:mm:ss}', false),
            (gen_random_uuid(), 'deepseek', 'DeepSeek', true, 7, 'https://api.deepseek.com/v1/', 'v1', NULL, '{now:yyyy-MM-dd HH:mm:ss}', '{now:yyyy-MM-dd HH:mm:ss}', false),
            (gen_random_uuid(), 'qwen', 'Qwen (Alibaba)', false, 8, 'https://dashscope.aliyuncs.com/api/v1/', 'v1', NULL, '{now:yyyy-MM-dd HH:mm:ss}', '{now:yyyy-MM-dd HH:mm:ss}', false),
            (gen_random_uuid(), 'baidu', 'Baidu Ernie', false, 9, 'https://aip.baidubce.com/rpc/2.0/', 'v1', NULL, '{now:yyyy-MM-dd HH:mm:ss}', '{now:yyyy-MM-dd HH:mm:ss}', false),
            (gen_random_uuid(), 'zhipu', 'Zhipu AI (GLM)', false, 10, 'https://open.bigmodel.cn/api/paas/v4/', 'v4', NULL, '{now:yyyy-MM-dd HH:mm:ss}', '{now:yyyy-MM-dd HH:mm:ss}', false),
            (gen_random_uuid(), 'apertus', 'Apertus (Swiss AI)', false, 11, 'https://app.apertus.ai/v1/', 'v1', NULL, '{now:yyyy-MM-dd HH:mm:ss}', '{now:yyyy-MM-dd HH:mm:ss}', false),
            (gen_random_uuid(), 'groq', 'Groq', false, 12, 'https://api.groq.com/openai/v1/', 'v1', NULL, '{now:yyyy-MM-dd HH:mm:ss}', '{now:yyyy-MM-dd HH:mm:ss}', false),
            (gen_random_uuid(), 'together', 'Together AI', false, 13, 'https://api.together.xyz/v1/', 'v1', NULL, '{now:yyyy-MM-dd HH:mm:ss}', '{now:yyyy-MM-dd HH:mm:ss}', false),
            (gen_random_uuid(), 'fireworks', 'Fireworks AI', false, 14, 'https://api.fireworks.ai/inference/v1/', 'v1', NULL, '{now:yyyy-MM-dd HH:mm:ss}', '{now:yyyy-MM-dd HH:mm:ss}', false),
            (gen_random_uuid(), 'kimi', 'Kimi (Moonshot AI)', false, 15, 'https://api.kimi.com/coding/v1/', 'v1', NULL, '{now:yyyy-MM-dd HH:mm:ss}', '{now:yyyy-MM-dd HH:mm:ss}', false);
        ");

        migrationBuilder.Sql($@"
            INSERT INTO llm_models (id, model_id, model_name, api_model_id, provider_id, is_enabled, is_default, cost_per_input_token, cost_per_output_token, max_tokens, context_window, category, create_time, update_time, is_deleted) VALUES
            (gen_random_uuid(), 'gpt-54', 'GPT-5.4', 'gpt-5.4', 'openai', true, true, 0.0025, 0.015, 128000, 1050000, 'powerful', '{now:yyyy-MM-dd HH:mm:ss}', '{now:yyyy-MM-dd HH:mm:ss}', false),
            (gen_random_uuid(), 'gpt-54-mini', 'GPT-5.4 Mini', 'gpt-5.4-mini', 'openai', true, false, 0.00075, 0.0045, 128000, 400000, 'fast', '{now:yyyy-MM-dd HH:mm:ss}', '{now:yyyy-MM-dd HH:mm:ss}', false),
            (gen_random_uuid(), 'gpt-54-nano', 'GPT-5.4 Nano', 'gpt-5.4-nano', 'openai', true, false, 0.0002, 0.00125, 128000, 400000, 'fast', '{now:yyyy-MM-dd HH:mm:ss}', '{now:yyyy-MM-dd HH:mm:ss}', false),
            (gen_random_uuid(), 'gpt-53-codex', 'GPT-5.3 Codex', 'gpt-5.3-codex', 'openai', true, false, 0.00175, 0.014, 128000, 400000, 'coding', '{now:yyyy-MM-dd HH:mm:ss}', '{now:yyyy-MM-dd HH:mm:ss}', false),
            (gen_random_uuid(), 'claude-opus-46', 'Claude Opus 4.6', 'claude-opus-4-6', 'anthropic', false, false, 0.005, 0.025, 128000, 1000000, 'powerful', '{now:yyyy-MM-dd HH:mm:ss}', '{now:yyyy-MM-dd HH:mm:ss}', false),
            (gen_random_uuid(), 'claude-sonnet-46', 'Claude Sonnet 4.6', 'claude-sonnet-4-6', 'anthropic', false, false, 0.003, 0.015, 64000, 1000000, 'balanced', '{now:yyyy-MM-dd HH:mm:ss}', '{now:yyyy-MM-dd HH:mm:ss}', false),
            (gen_random_uuid(), 'claude-haiku-45', 'Claude Haiku 4.5', 'claude-haiku-4-5-20251001', 'anthropic', false, false, 0.001, 0.005, 64000, 200000, 'fast', '{now:yyyy-MM-dd HH:mm:ss}', '{now:yyyy-MM-dd HH:mm:ss}', false),
            (gen_random_uuid(), 'gemini-31-pro', 'Gemini 3.1 Pro', 'gemini-3.1-pro-preview', 'google', true, true, 0.002, 0.012, 64000, 2000000, 'powerful', '{now:yyyy-MM-dd HH:mm:ss}', '{now:yyyy-MM-dd HH:mm:ss}', false),
            (gen_random_uuid(), 'gemini-3-flash', 'Gemini 3 Flash', 'gemini-3-flash-preview', 'google', true, false, 0.0005, 0.003, 64000, 1000000, 'balanced', '{now:yyyy-MM-dd HH:mm:ss}', '{now:yyyy-MM-dd HH:mm:ss}', false),
            (gen_random_uuid(), 'gemini-31-flash-lite', 'Gemini 3.1 Flash Lite', 'gemini-3.1-flash-lite-preview', 'google', true, false, 0.0001, 0.0004, 8192, 1000000, 'fast', '{now:yyyy-MM-dd HH:mm:ss}', '{now:yyyy-MM-dd HH:mm:ss}', false),
            (gen_random_uuid(), 'gemini-25-flash', 'Gemini 2.5 Flash', 'gemini-2.5-flash', 'google', true, false, 0.0003, 0.0025, 8192, 1000000, 'fast', '{now:yyyy-MM-dd HH:mm:ss}', '{now:yyyy-MM-dd HH:mm:ss}', false),
            (gen_random_uuid(), 'mistral-large-3', 'Mistral Large 3', 'mistral-large-2512', 'mistral', false, false, 0.0005, 0.0015, 8192, 262000, 'powerful', '{now:yyyy-MM-dd HH:mm:ss}', '{now:yyyy-MM-dd HH:mm:ss}', false),
            (gen_random_uuid(), 'mistral-small-4', 'Mistral Small 4', 'mistral-small-2603', 'mistral', false, false, 0.00015, 0.00045, 8192, 262000, 'fast', '{now:yyyy-MM-dd HH:mm:ss}', '{now:yyyy-MM-dd HH:mm:ss}', false),
            (gen_random_uuid(), 'magistral-medium', 'Magistral Medium', 'magistral-medium-2509', 'mistral', false, false, 0.002, 0.005, 8192, 40000, 'reasoning', '{now:yyyy-MM-dd HH:mm:ss}', '{now:yyyy-MM-dd HH:mm:ss}', false),
            (gen_random_uuid(), 'devstral-2', 'Devstral 2', 'devstral-2512', 'mistral', false, false, 0.0004, 0.0009, 16384, 262000, 'coding', '{now:yyyy-MM-dd HH:mm:ss}', '{now:yyyy-MM-dd HH:mm:ss}', false),
            (gen_random_uuid(), 'command-a', 'Command A', 'command-a', 'cohere', false, false, 0.003, 0.015, 4096, 128000, 'powerful', '{now:yyyy-MM-dd HH:mm:ss}', '{now:yyyy-MM-dd HH:mm:ss}', false),
            (gen_random_uuid(), 'command-a-reasoning', 'Command A Reasoning', 'command-a-reasoning', 'cohere', false, false, 0.004, 0.02, 4096, 128000, 'reasoning', '{now:yyyy-MM-dd HH:mm:ss}', '{now:yyyy-MM-dd HH:mm:ss}', false),
            (gen_random_uuid(), 'command-r-plus', 'Command R+', 'command-r-plus-08-2024', 'cohere', false, false, 0.003, 0.015, 4096, 128000, 'balanced', '{now:yyyy-MM-dd HH:mm:ss}', '{now:yyyy-MM-dd HH:mm:ss}', false),
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
            (gen_random_uuid(), 'kimi-for-coding', 'Kimi K2.5 for Coding', 'kimi-for-coding', 'kimi', false, false, 0.00045, 0.0022, 65535, 262144, 'coding', '{now:yyyy-MM-dd HH:mm:ss}', '{now:yyyy-MM-dd HH:mm:ss}', false);
        ");
    }
}