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
            (gen_random_uuid(), 'deepseek', 'DeepSeek', false, 7, 'https://api.deepseek.com/v1/', 'v1', NULL, '{now:yyyy-MM-dd HH:mm:ss}', '{now:yyyy-MM-dd HH:mm:ss}', false),
            (gen_random_uuid(), 'qwen', 'Qwen (Alibaba)', false, 8, 'https://dashscope.aliyuncs.com/api/v1/', 'v1', NULL, '{now:yyyy-MM-dd HH:mm:ss}', '{now:yyyy-MM-dd HH:mm:ss}', false),
            (gen_random_uuid(), 'baidu', 'Baidu Ernie', false, 9, 'https://aip.baidubce.com/rpc/2.0/', 'v1', NULL, '{now:yyyy-MM-dd HH:mm:ss}', '{now:yyyy-MM-dd HH:mm:ss}', false),
            (gen_random_uuid(), 'zhipu', 'Zhipu AI (GLM)', false, 10, 'https://open.bigmodel.cn/api/paas/v4/', 'v4', NULL, '{now:yyyy-MM-dd HH:mm:ss}', '{now:yyyy-MM-dd HH:mm:ss}', false);
        ");

        migrationBuilder.Sql($@"
            INSERT INTO llm_models (id, model_id, model_name, api_model_id, provider_id, is_enabled, is_default, cost_per_input_token, cost_per_output_token, max_tokens, context_window, category, create_time, update_time, is_deleted) VALUES 
            (gen_random_uuid(), 'gpt-35-turbo', 'GPT-3.5 Turbo', 'gpt-3.5-turbo', 'openai', true, false, 0.0005, 0.0015, 4096, 16385, 'fast', '{now:yyyy-MM-dd HH:mm:ss}', '{now:yyyy-MM-dd HH:mm:ss}', false),
            (gen_random_uuid(), 'gpt-4', 'GPT-4', 'gpt-4', 'openai', false, false, 0.03, 0.06, 4096, 8192, 'powerful', '{now:yyyy-MM-dd HH:mm:ss}', '{now:yyyy-MM-dd HH:mm:ss}', false),
            (gen_random_uuid(), 'gpt-4o', 'GPT-4o', 'gpt-4o', 'openai', true, true, 0.005, 0.015, 4096, 128000, 'powerful', '{now:yyyy-MM-dd HH:mm:ss}', '{now:yyyy-MM-dd HH:mm:ss}', false),
            (gen_random_uuid(), 'gpt-4o-mini', 'GPT-4o Mini', 'gpt-4o-mini', 'openai', true, false, 0.00015, 0.0006, 16384, 128000, 'balanced', '{now:yyyy-MM-dd HH:mm:ss}', '{now:yyyy-MM-dd HH:mm:ss}', false),
            (gen_random_uuid(), 'claude-haiku-35', 'Claude 3.5 Haiku', 'claude-3-5-haiku-20241022', 'anthropic', false, false, 0.00025, 0.00125, 4096, 200000, 'fast', '{now:yyyy-MM-dd HH:mm:ss}', '{now:yyyy-MM-dd HH:mm:ss}', false),
            (gen_random_uuid(), 'claude-sonnet-45', 'Claude Sonnet 4.5', 'claude-sonnet-4-5-20250929', 'anthropic', false, false, 0.003, 0.015, 4096, 200000, 'balanced', '{now:yyyy-MM-dd HH:mm:ss}', '{now:yyyy-MM-dd HH:mm:ss}', false),
            (gen_random_uuid(), 'claude-opus-45', 'Claude Opus 4.5', 'claude-opus-4-5-20251101', 'anthropic', false, false, 0.015, 0.075, 4096, 200000, 'powerful', '{now:yyyy-MM-dd HH:mm:ss}', '{now:yyyy-MM-dd HH:mm:ss}', false),
            (gen_random_uuid(), 'gemini-25-flash', 'Gemini 2.5 Flash', 'gemini-2.5-flash', 'google', true, false, 0.00025, 0.0005, 8192, 1000000, 'fast', '{now:yyyy-MM-dd HH:mm:ss}', '{now:yyyy-MM-dd HH:mm:ss}', false),
            (gen_random_uuid(), 'gemini-25-pro', 'Gemini 2.5 Pro', 'gemini-2.5-pro', 'google', false, false, 0.0025, 0.0075, 8192, 1000000, 'balanced', '{now:yyyy-MM-dd HH:mm:ss}', '{now:yyyy-MM-dd HH:mm:ss}', false),
            (gen_random_uuid(), 'gemini-25-ultra', 'Gemini 2.5 Ultra', 'gemini-2.5-ultra', 'google', false, false, 0.005, 0.015, 8192, 1000000, 'powerful', '{now:yyyy-MM-dd HH:mm:ss}', '{now:yyyy-MM-dd HH:mm:ss}', false),
            (gen_random_uuid(), 'mistral-large', 'Mistral Large', 'mistral-large-2407', 'mistral', false, false, 0.003, 0.009, 8192, 128000, 'powerful', '{now:yyyy-MM-dd HH:mm:ss}', '{now:yyyy-MM-dd HH:mm:ss}', false),
            (gen_random_uuid(), 'mistral-small', 'Mistral Small', 'mistral-small-2409', 'mistral', false, false, 0.001, 0.003, 8192, 128000, 'balanced', '{now:yyyy-MM-dd HH:mm:ss}', '{now:yyyy-MM-dd HH:mm:ss}', false),
            (gen_random_uuid(), 'command-r-plus', 'Command R+', 'command-r-plus-08-2024', 'cohere', false, false, 0.003, 0.015, 4096, 128000, 'powerful', '{now:yyyy-MM-dd HH:mm:ss}', '{now:yyyy-MM-dd HH:mm:ss}', false),
            (gen_random_uuid(), 'command-r', 'Command R', 'command-r-08-2024', 'cohere', false, false, 0.0015, 0.0075, 4096, 128000, 'balanced', '{now:yyyy-MM-dd HH:mm:ss}', '{now:yyyy-MM-dd HH:mm:ss}', false),
            (gen_random_uuid(), 'deepseek-chat', 'DeepSeek V3.1 Chat', 'deepseek-chat', 'deepseek', false, false, 0.00014, 0.00028, 8192, 128000, 'balanced', '{now:yyyy-MM-dd HH:mm:ss}', '{now:yyyy-MM-dd HH:mm:ss}', false),
            (gen_random_uuid(), 'deepseek-reasoner', 'DeepSeek V3.1 Reasoner', 'deepseek-reasoner', 'deepseek', false, false, 0.00014, 0.00028, 8192, 128000, 'reasoning', '{now:yyyy-MM-dd HH:mm:ss}', '{now:yyyy-MM-dd HH:mm:ss}', false),
            (gen_random_uuid(), 'qwen-turbo', 'Qwen Turbo', 'qwen-turbo', 'qwen', false, false, 0.0002, 0.0006, 1500, 6000, 'fast', '{now:yyyy-MM-dd HH:mm:ss}', '{now:yyyy-MM-dd HH:mm:ss}', false),
            (gen_random_uuid(), 'qwen-plus', 'Qwen Plus', 'qwen-plus', 'qwen', false, false, 0.0004, 0.002, 2000, 30000, 'balanced', '{now:yyyy-MM-dd HH:mm:ss}', '{now:yyyy-MM-dd HH:mm:ss}', false),
            (gen_random_uuid(), 'ernie-4', 'ERNIE-4.0-8K', 'ernie-4.0-8k', 'baidu', false, false, 0.0012, 0.002, 2048, 8000, 'powerful', '{now:yyyy-MM-dd HH:mm:ss}', '{now:yyyy-MM-dd HH:mm:ss}', false),
            (gen_random_uuid(), 'ernie-35', 'ERNIE-3.5-8K', 'ernie-3.5-8k', 'baidu', false, false, 0.0008, 0.002, 2048, 8000, 'balanced', '{now:yyyy-MM-dd HH:mm:ss}', '{now:yyyy-MM-dd HH:mm:ss}', false),
            (gen_random_uuid(), 'glm-4', 'GLM-4', 'glm-4', 'zhipu', false, false, 0.001, 0.003, 4095, 128000, 'powerful', '{now:yyyy-MM-dd HH:mm:ss}', '{now:yyyy-MM-dd HH:mm:ss}', false),
            (gen_random_uuid(), 'glm-4-flash', 'GLM-4-Flash', 'glm-4-flash', 'zhipu', false, false, 0.0001, 0.0003, 4095, 128000, 'fast', '{now:yyyy-MM-dd HH:mm:ss}', '{now:yyyy-MM-dd HH:mm:ss}', false);
        ");
    }
}