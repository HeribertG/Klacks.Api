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
            (gen_random_uuid(), 'zhipu', 'Zhipu AI (GLM)', false, 10, 'https://open.bigmodel.cn/api/paas/v4/', 'v4', NULL, '{now:yyyy-MM-dd HH:mm:ss}', '{now:yyyy-MM-dd HH:mm:ss}', false);
        ");

        migrationBuilder.Sql($@"
            INSERT INTO llm_models (id, model_id, model_name, api_model_id, provider_id, is_enabled, is_default, cost_per_input_token, cost_per_output_token, max_tokens, context_window, category, create_time, update_time, is_deleted) VALUES 
            (gen_random_uuid(), 'gpt-51', 'GPT-5.1', 'gpt-5.1', 'openai', true, true, 0.003, 0.012, 16384, 128000, 'powerful', '{now:yyyy-MM-dd HH:mm:ss}', '{now:yyyy-MM-dd HH:mm:ss}', false),
            (gen_random_uuid(), 'gpt-51-instant', 'GPT-5.1 Instant', 'gpt-5.1-instant', 'openai', true, false, 0.0003, 0.0012, 16384, 128000, 'fast', '{now:yyyy-MM-dd HH:mm:ss}', '{now:yyyy-MM-dd HH:mm:ss}', false),
            (gen_random_uuid(), 'gpt-51-codex', 'GPT-5.1 Codex', 'gpt-5.1-codex', 'openai', true, false, 0.005, 0.02, 32768, 128000, 'coding', '{now:yyyy-MM-dd HH:mm:ss}', '{now:yyyy-MM-dd HH:mm:ss}', false),
            (gen_random_uuid(), 'gpt-51-codex-mini', 'GPT-5.1 Codex Mini', 'gpt-5.1-codex-mini', 'openai', true, false, 0.001, 0.004, 16384, 128000, 'coding', '{now:yyyy-MM-dd HH:mm:ss}', '{now:yyyy-MM-dd HH:mm:ss}', false),
            (gen_random_uuid(), 'claude-haiku-35', 'Claude 3.5 Haiku', 'claude-3-5-haiku-20241022', 'anthropic', false, false, 0.00025, 0.00125, 4096, 200000, 'fast', '{now:yyyy-MM-dd HH:mm:ss}', '{now:yyyy-MM-dd HH:mm:ss}', false),
            (gen_random_uuid(), 'claude-sonnet-45', 'Claude Sonnet 4.5', 'claude-sonnet-4-5-20250929', 'anthropic', false, false, 0.003, 0.015, 4096, 200000, 'balanced', '{now:yyyy-MM-dd HH:mm:ss}', '{now:yyyy-MM-dd HH:mm:ss}', false),
            (gen_random_uuid(), 'claude-opus-45', 'Claude Opus 4.5', 'claude-opus-4-5-20251101', 'anthropic', false, false, 0.015, 0.075, 4096, 200000, 'powerful', '{now:yyyy-MM-dd HH:mm:ss}', '{now:yyyy-MM-dd HH:mm:ss}', false),
            (gen_random_uuid(), 'gemini-3-pro', 'Gemini 3 Pro', 'gemini-3-pro-preview', 'google', true, true, 0.00125, 0.005, 8192, 1000000, 'powerful', '{now:yyyy-MM-dd HH:mm:ss}', '{now:yyyy-MM-dd HH:mm:ss}', false),
            (gen_random_uuid(), 'gemini-25-pro', 'Gemini 2.5 Pro', 'gemini-2.5-pro', 'google', true, false, 0.00125, 0.005, 8192, 1000000, 'balanced', '{now:yyyy-MM-dd HH:mm:ss}', '{now:yyyy-MM-dd HH:mm:ss}', false),
            (gen_random_uuid(), 'gemini-25-flash', 'Gemini 2.5 Flash', 'gemini-2.5-flash', 'google', true, false, 0.00025, 0.0005, 8192, 1000000, 'fast', '{now:yyyy-MM-dd HH:mm:ss}', '{now:yyyy-MM-dd HH:mm:ss}', false),
            (gen_random_uuid(), 'mistral-medium-3', 'Mistral Medium 3', 'mistral-medium-2505', 'mistral', false, false, 0.002, 0.006, 8192, 128000, 'powerful', '{now:yyyy-MM-dd HH:mm:ss}', '{now:yyyy-MM-dd HH:mm:ss}', false),
            (gen_random_uuid(), 'mistral-small-31', 'Mistral Small 3.1', 'mistral-small-2503', 'mistral', false, false, 0.001, 0.003, 8192, 128000, 'balanced', '{now:yyyy-MM-dd HH:mm:ss}', '{now:yyyy-MM-dd HH:mm:ss}', false),
            (gen_random_uuid(), 'magistral-medium', 'Magistral Medium', 'magistral-medium-2509', 'mistral', false, false, 0.003, 0.009, 8192, 128000, 'reasoning', '{now:yyyy-MM-dd HH:mm:ss}', '{now:yyyy-MM-dd HH:mm:ss}', false),
            (gen_random_uuid(), 'devstral-medium', 'Devstral Medium', 'devstral-medium-2507', 'mistral', false, false, 0.002, 0.006, 16384, 128000, 'coding', '{now:yyyy-MM-dd HH:mm:ss}', '{now:yyyy-MM-dd HH:mm:ss}', false),
            (gen_random_uuid(), 'command-a', 'Command A', 'command-a', 'cohere', false, false, 0.003, 0.015, 4096, 128000, 'powerful', '{now:yyyy-MM-dd HH:mm:ss}', '{now:yyyy-MM-dd HH:mm:ss}', false),
            (gen_random_uuid(), 'command-a-reasoning', 'Command A Reasoning', 'command-a-reasoning', 'cohere', false, false, 0.004, 0.02, 4096, 128000, 'reasoning', '{now:yyyy-MM-dd HH:mm:ss}', '{now:yyyy-MM-dd HH:mm:ss}', false),
            (gen_random_uuid(), 'command-r-plus', 'Command R+', 'command-r-plus-08-2024', 'cohere', false, false, 0.003, 0.015, 4096, 128000, 'balanced', '{now:yyyy-MM-dd HH:mm:ss}', '{now:yyyy-MM-dd HH:mm:ss}', false),
            (gen_random_uuid(), 'deepseek-chat', 'DeepSeek V3.2 Chat', 'deepseek-chat', 'deepseek', true, false, 0.00014, 0.00028, 8192, 128000, 'balanced', '{now:yyyy-MM-dd HH:mm:ss}', '{now:yyyy-MM-dd HH:mm:ss}', false),
            (gen_random_uuid(), 'deepseek-reasoner', 'DeepSeek V3.2 Reasoner', 'deepseek-reasoner', 'deepseek', true, false, 0.00055, 0.00219, 8192, 128000, 'reasoning', '{now:yyyy-MM-dd HH:mm:ss}', '{now:yyyy-MM-dd HH:mm:ss}', false),
            (gen_random_uuid(), 'qwen3-max', 'Qwen3 Max', 'qwen3-max', 'qwen', false, false, 0.002, 0.006, 8192, 128000, 'powerful', '{now:yyyy-MM-dd HH:mm:ss}', '{now:yyyy-MM-dd HH:mm:ss}', false),
            (gen_random_uuid(), 'qwen3-32b', 'Qwen3 32B', 'qwen3-32b', 'qwen', false, false, 0.0008, 0.002, 8192, 128000, 'balanced', '{now:yyyy-MM-dd HH:mm:ss}', '{now:yyyy-MM-dd HH:mm:ss}', false),
            (gen_random_uuid(), 'ernie-5', 'ERNIE 5.0', 'ernie-5.0', 'baidu', false, false, 0.002, 0.006, 8192, 128000, 'powerful', '{now:yyyy-MM-dd HH:mm:ss}', '{now:yyyy-MM-dd HH:mm:ss}', false),
            (gen_random_uuid(), 'ernie-45-vl', 'ERNIE 4.5 VL', 'ernie-4.5-vl', 'baidu', false, false, 0.0015, 0.004, 8192, 128000, 'balanced', '{now:yyyy-MM-dd HH:mm:ss}', '{now:yyyy-MM-dd HH:mm:ss}', false),
            (gen_random_uuid(), 'glm-46', 'GLM-4.6', 'glm-4.6', 'zhipu', false, false, 0.002, 0.006, 8192, 200000, 'powerful', '{now:yyyy-MM-dd HH:mm:ss}', '{now:yyyy-MM-dd HH:mm:ss}', false),
            (gen_random_uuid(), 'glm-45', 'GLM-4.5', 'glm-4.5', 'zhipu', false, false, 0.001, 0.003, 8192, 128000, 'balanced', '{now:yyyy-MM-dd HH:mm:ss}', '{now:yyyy-MM-dd HH:mm:ss}', false);
        ");
    }
}