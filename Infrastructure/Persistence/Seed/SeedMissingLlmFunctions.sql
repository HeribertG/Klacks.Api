-- Fehlende LLM Function Definitions fuer Chat-LLM
-- Diese Funktionen waren bisher nur als Skills verfuegbar.
-- Sie werden jetzt in die DB aufgenommen mit ExecutionType "Skill",
-- damit der LLMFunctionExecutor sie via SkillBridge ausfuehrt.

-- 1. get_user_permissions
INSERT INTO llm_function_definitions
  (id, name, description, parameters_json, required_permission, execution_type, category, is_enabled, sort_order, create_time, current_user_created, is_deleted)
SELECT gen_random_uuid(),
       'get_user_permissions',
       'Returns the current user''s permissions and role. Use this to understand what actions the user is allowed to perform before attempting operations that require specific permissions.',
       '[]',
       NULL, 'Skill', 'backend', true, 215, NOW(), 'Migration', false
WHERE NOT EXISTS (SELECT 1 FROM llm_function_definitions WHERE name = 'get_user_permissions' AND is_deleted = false);

-- 2. get_ai_soul
INSERT INTO llm_function_definitions
  (id, name, description, parameters_json, required_permission, execution_type, category, is_enabled, sort_order, create_time, current_user_created, is_deleted)
SELECT gen_random_uuid(),
       'get_ai_soul',
       'Retrieves the AI assistant''s personality definition (soul). The soul defines the assistant''s identity, values, boundaries, and communication style.',
       '[]',
       'CanViewSettings', 'Skill', 'backend', true, 220, NOW(), 'Migration', false
WHERE NOT EXISTS (SELECT 1 FROM llm_function_definitions WHERE name = 'get_ai_soul' AND is_deleted = false);

-- 3. update_ai_soul
INSERT INTO llm_function_definitions
  (id, name, description, parameters_json, required_permission, execution_type, category, is_enabled, sort_order, create_time, current_user_created, is_deleted)
SELECT gen_random_uuid(),
       'update_ai_soul',
       'Updates the AI assistant''s personality definition (soul). The soul defines the assistant''s identity, values, boundaries, and communication style. This affects how the assistant behaves and responds in all conversations.',
       '[{"name":"soul","type":"string","description":"The complete soul text defining the AI''s personality, values, boundaries, and communication style.","required":true},{"name":"name","type":"string","description":"A short name for this soul definition (e.g. ''Klacks Planungsassistent'').","required":false}]',
       'CanEditSettings', 'Skill', 'backend', true, 225, NOW(), 'Migration', false
WHERE NOT EXISTS (SELECT 1 FROM llm_function_definitions WHERE name = 'update_ai_soul' AND is_deleted = false);

-- 4. add_ai_memory
INSERT INTO llm_function_definitions
  (id, name, description, parameters_json, required_permission, execution_type, category, is_enabled, sort_order, create_time, current_user_created, is_deleted)
SELECT gen_random_uuid(),
       'add_ai_memory',
       'Adds a new persistent memory entry for the AI assistant. Memories persist across all conversations and help the assistant remember important facts, user preferences, and system knowledge.',
       '[{"name":"key","type":"string","description":"Short identifier or title for the memory entry.","required":true},{"name":"content","type":"string","description":"The actual memory content to remember.","required":true},{"name":"category","type":"string","description":"Category of the memory.","required":false,"enum":["user_preference","system_knowledge","learned_fact","workflow","context"]},{"name":"importance","type":"integer","description":"Importance level from 1 (low) to 10 (high).","required":false}]',
       'Admin', 'Skill', 'backend', true, 230, NOW(), 'Migration', false
WHERE NOT EXISTS (SELECT 1 FROM llm_function_definitions WHERE name = 'add_ai_memory' AND is_deleted = false);

-- 5. get_ai_memories
INSERT INTO llm_function_definitions
  (id, name, description, parameters_json, required_permission, execution_type, category, is_enabled, sort_order, create_time, current_user_created, is_deleted)
SELECT gen_random_uuid(),
       'get_ai_memories',
       'Retrieves the AI assistant''s persistent memory entries. Can filter by category or search by keyword. Returns all memories sorted by importance.',
       '[{"name":"category","type":"string","description":"Filter by category.","required":false,"enum":["user_preference","system_knowledge","learned_fact","workflow","context"]},{"name":"searchTerm","type":"string","description":"Search term to filter memories by key or content.","required":false}]',
       'Admin', 'Skill', 'backend', true, 235, NOW(), 'Migration', false
WHERE NOT EXISTS (SELECT 1 FROM llm_function_definitions WHERE name = 'get_ai_memories' AND is_deleted = false);

-- 6. update_ai_memory
INSERT INTO llm_function_definitions
  (id, name, description, parameters_json, required_permission, execution_type, category, is_enabled, sort_order, create_time, current_user_created, is_deleted)
SELECT gen_random_uuid(),
       'update_ai_memory',
       'Updates an existing persistent memory entry. You can update the key, content, category, or importance of a specific memory.',
       '[{"name":"memoryId","type":"string","description":"The ID of the memory entry to update.","required":true},{"name":"key","type":"string","description":"New short identifier or title for the memory entry.","required":false},{"name":"content","type":"string","description":"New memory content.","required":false},{"name":"category","type":"string","description":"New category.","required":false,"enum":["user_preference","system_knowledge","learned_fact","workflow","context"]},{"name":"importance","type":"integer","description":"New importance level from 1 (low) to 10 (high).","required":false}]',
       'Admin', 'Skill', 'backend', true, 240, NOW(), 'Migration', false
WHERE NOT EXISTS (SELECT 1 FROM llm_function_definitions WHERE name = 'update_ai_memory' AND is_deleted = false);

-- 7. delete_ai_memory
INSERT INTO llm_function_definitions
  (id, name, description, parameters_json, required_permission, execution_type, category, is_enabled, sort_order, create_time, current_user_created, is_deleted)
SELECT gen_random_uuid(),
       'delete_ai_memory',
       'Deletes a persistent memory entry by its ID. This permanently removes the memory from the AI assistant''s knowledge base.',
       '[{"name":"memoryId","type":"string","description":"The ID of the memory entry to delete.","required":true}]',
       'Admin', 'Skill', 'backend', true, 245, NOW(), 'Migration', false
WHERE NOT EXISTS (SELECT 1 FROM llm_function_definitions WHERE name = 'delete_ai_memory' AND is_deleted = false);

-- 8. get_ai_guidelines
INSERT INTO llm_function_definitions
  (id, name, description, parameters_json, required_permission, execution_type, category, is_enabled, sort_order, create_time, current_user_created, is_deleted)
SELECT gen_random_uuid(),
       'get_ai_guidelines',
       'Retrieves the AI assistant''s guidelines. Guidelines define rules for behavior, function usage, and permission handling.',
       '[]',
       'CanViewSettings', 'Skill', 'backend', true, 250, NOW(), 'Migration', false
WHERE NOT EXISTS (SELECT 1 FROM llm_function_definitions WHERE name = 'get_ai_guidelines' AND is_deleted = false);

-- 9. update_ai_guidelines
INSERT INTO llm_function_definitions
  (id, name, description, parameters_json, required_permission, execution_type, category, is_enabled, sort_order, create_time, current_user_created, is_deleted)
SELECT gen_random_uuid(),
       'update_ai_guidelines',
       'Updates the AI assistant''s guidelines. Guidelines define rules for behavior, function usage, and permission handling. This affects how the assistant operates in all conversations.',
       '[{"name":"guidelines","type":"string","description":"The complete guidelines text defining the AI''s behavioral rules.","required":true},{"name":"name","type":"string","description":"A short name for this guidelines set.","required":false}]',
       'CanEditSettings', 'Skill', 'backend', true, 255, NOW(), 'Migration', false
WHERE NOT EXISTS (SELECT 1 FROM llm_function_definitions WHERE name = 'update_ai_guidelines' AND is_deleted = false);
