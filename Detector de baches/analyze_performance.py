import os
import re

search_path = r"g:\Github\Software de simulacion\Recogedor-de-basura\Detector de baches\Assets\Scripts"
output_file = r"g:\Github\Software de simulacion\Recogedor-de-basura\Detector de baches\performance_analysis_report.txt"

def extract_method_blocks(code):
    """
    Rudimentary parser to extract contents of Update, FixedUpdate, LateUpdate.
    Returns a dict with method name as key and the body as value.
    """
    blocks = {}
    lines = code.split('\n')
    
    in_target_method = False
    current_method = ""
    brace_count = 0
    method_content = []
    
    for i, line in enumerate(lines):
        line_num = i + 1
        stripped = line.strip()
        
        if not in_target_method:
            match = re.search(r'void\s+(Update|FixedUpdate|LateUpdate)\s*\(', stripped)
            if match:
                in_target_method = True
                current_method = match.group(1)
                brace_count = 0
                method_content = []
                if '{' in stripped:
                    brace_count += stripped.count('{') - stripped.count('}')
                    if brace_count <= 0:
                        in_target_method = False # One-liner maybe
        else:
            method_content.append((line_num, line))
            brace_count += stripped.count('{') - stripped.count('}')
            if brace_count <= 0 and '}' in stripped:
                blocks[current_method] = method_content
                in_target_method = False
                
    return blocks

issues_found = []

for root, dirs, files in os.walk(search_path):
    for file in files:
        if file.endswith(".cs"):
            filepath = os.path.join(root, file)
            try:
                with open(filepath, 'r', encoding='utf-8-sig') as f:
                    content = f.read()
            except Exception as e:
                continue
                
            file_issues = []
            
            # Check global bad patterns
            lines = content.split('\n')
            for i, line in enumerate(lines):
                line_num = i + 1
                stripped = line.strip()
                if stripped.startswith('//'):
                    continue
                    
                if '.material' in stripped and '.sharedMaterial' not in stripped and 'color' not in stripped.lower():
                    # .material getter clones the material
                    file_issues.append((line_num, f"Uses '.material' which can leak memory if not Destroyed: {stripped}"))
                if 'FindObjectsOfType' in stripped:
                    file_issues.append((line_num, f"Heavy operation FindObjectsOfType: {stripped}"))
                # if 'Camera.main' in stripped:
                #     file_issues.append((line_num, f"Camera.main usage (can be slow if in a loop/Update): {stripped}"))
                    
            # Check Update methods
            method_blocks = extract_method_blocks(content)
            for method, block in method_blocks.items():
                for line_num, line in block:
                    stripped = line.strip()
                    if stripped.startswith('//'):
                        continue
                        
                    if 'GetComponent' in stripped:
                        file_issues.append((line_num, f"[{method}] GetComponent called: {stripped}"))
                    if 'FindObject' in stripped or 'GameObject.Find' in stripped:
                        file_issues.append((line_num, f"[{method}] FindObject called: {stripped}"))
                    if 'Instantiate' in stripped:
                        file_issues.append((line_num, f"[{method}] Instantiate called: {stripped}"))
                    if 'Destroy' in stripped:
                        file_issues.append((line_num, f"[{method}] Destroy called (can cause GC spikes): {stripped}"))
                    if 'new ' in stripped and 'new Vector' not in stripped and 'new WaitFor' not in stripped and not stripped.startswith('//'):
                        # rudimentary check for allocations 
                        file_issues.append((line_num, f"[{method}] Object allocation ('new'): {stripped}"))
                    if 'Debug.Log' in stripped:
                        file_issues.append((line_num, f"[{method}] Debug.Log generates garbage: {stripped}"))
                        
            if file_issues:
                issues_found.append((filepath, file_issues))

with open(output_file, 'w', encoding='utf-8') as f:
    f.write("=== Performance Analysis Report ===\n\n")
    if not issues_found:
        f.write("No major issues found based on static analysis.\n")
    for filepath, issues in issues_found:
        rel_path = filepath.replace(search_path, "").strip("\\")
        f.write(f"File: {rel_path}\n")
        f.write("-" * 40 + "\n")
        for line_num, desc in issues:
            f.write(f"  Line {line_num}: {desc}\n")
        f.write("\n")
        
print(f"Analysis complete. Report saved to {output_file}")
