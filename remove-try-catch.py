#!/usr/bin/env python3
import os
import re

def remove_unnecessary_try_catch(file_path):
    with open(file_path, 'r', encoding='utf-8') as f:
        content = f.read()
    
    if 'BaseHandler' not in content:
        return False
    
    original_content = content
    
    # Pattern to match try-catch blocks that only rethrow
    # This pattern looks for try-catch where catch only logs and rethrows
    simple_try_catch_pattern = r'''
        try\s*\{
            (.*?)
        \}\s*
        catch\s*\([^)]*\)\s*\{
            (?:[^{}]*_logger\.Log[^;]*;[^{}]*)?
            \s*throw;\s*
        \}
    '''
    
    # Remove simple try-catch blocks that only rethrow
    def replace_simple_try_catch(match):
        inner_content = match.group(1).strip()
        return inner_content
    
    content = re.sub(simple_try_catch_pattern, replace_simple_try_catch, content, flags=re.DOTALL | re.VERBOSE)
    
    # Also look for try-catch that just wraps a single operation
    single_operation_pattern = r'''
        try\s*\{
            \s*(.*?)\s*
            return\s+(.*?);
        \}\s*
        catch\s*\([^)]*\)\s*\{
            (?:[^{}]*_logger\.Log[^;]*;[^{}]*)?
            \s*throw;\s*
        \}
    '''
    
    def replace_single_operation(match):
        operation = match.group(1).strip()
        return_expr = match.group(2).strip()
        if operation:
            return f"{operation}\n        return {return_expr};"
        else:
            return f"return {return_expr};"
    
    content = re.sub(single_operation_pattern, replace_single_operation, content, flags=re.DOTALL | re.VERBOSE)
    
    # Clean up extra whitespace
    content = re.sub(r'\n\s*\n\s*\n', '\n\n', content)
    
    if content != original_content:
        with open(file_path, 'w', encoding='utf-8') as f:
            f.write(content)
        return True
    
    return False

def main():
    handlers_dir = '/mnt/c/SourceCode/Klacks.Api/Application/Handlers'
    fixed_count = 0
    
    for root, dirs, files in os.walk(handlers_dir):
        for file in files:
            if file.endswith('Handler.cs') and 'BaseHandler' not in file:
                file_path = os.path.join(root, file)
                if remove_unnecessary_try_catch(file_path):
                    print(f"Removed try-catch from {file_path}")
                    fixed_count += 1
    
    print(f"\nTotal handlers cleaned: {fixed_count}")

if __name__ == "__main__":
    main()