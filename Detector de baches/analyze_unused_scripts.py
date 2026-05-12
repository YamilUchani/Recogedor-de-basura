
import os
import glob
import re

# Path to the project root
PROJECT_ROOT = r"g:\Github\Software de simulacion\Recogedor-de-basura\Detector de baches"
SCRIPTS_DIR = r"g:\Github\Software de simulacion\Recogedor-de-basura\Detector de baches\Assets\Assets\Scripts"

def get_all_scripts(scripts_dir):
    """Get all C# scripts in the Scripts directory"""
    all_scripts = set()
    for root, dirs, files in os.walk(scripts_dir):
        for file in files:
            if file.endswith(".cs") and not file.endswith(".meta"):
                all_scripts.add(file)
    return all_scripts

def get_script_guids(root_dir):
    """Build a mapping of GUID to script name"""
    guid_map = {}
    for root, dirs, files in os.walk(root_dir):
        # Skip large directories
        dirs[:] = [d for d in dirs if d not in ["Library", "Temp", "Obj", "Logs", "Builds", ".git", "PackageCache"]]

        for file in files:
            if file.endswith(".cs.meta"):
                full_path = os.path.join(root, file)
                try:
                    with open(full_path, 'r', encoding='utf-8') as f:
                        content = f.read()
                        match = re.search(r'guid: ([a-f0-9]{32})', content)
                        if match:
                            guid = match.group(1)
                            script_path = full_path[:-5]
                            script_name = os.path.basename(script_path)
                            guid_map[guid] = script_name
                except Exception as e:
                    print(f"Error reading {full_path}: {e}")
    return guid_map

def get_used_scripts_in_scenes(root_dir, guid_map):
    """Get all scripts used in scene files"""
    used_scripts = set()
    
    for root, dirs, files in os.walk(root_dir):
        # Skip large directories
        dirs[:] = [d for d in dirs if d not in ["Library", "Temp", "Obj", "Logs", "Builds", ".git", "PackageCache"]]
            
        for file in files:
            if file.endswith(".unity"):
                # Skip examples and samples
                if "examples" in root.lower() or "samples" in root.lower():
                    continue

                scene_path = os.path.join(root, file)
                
                try:
                    with open(scene_path, 'r', encoding='utf-8') as f:
                        content = f.read()
                        matches = re.finditer(r'm_Script: \{fileID: \d+, guid: ([a-f0-9]{32}), type: \d+\}', content)
                        for match in matches:
                            guid = match.group(1)
                            if guid in guid_map:
                                used_scripts.add(guid_map[guid])
                except Exception as e:
                    print(f"Error reading {scene_path}: {e}")
    
    return used_scripts

def get_used_scripts_in_prefabs(root_dir, guid_map):
    """Get all scripts used in prefab files"""
    used_scripts = set()
    
    for root, dirs, files in os.walk(root_dir):
        # Skip large directories
        dirs[:] = [d for d in dirs if d not in ["Library", "Temp", "Obj", "Logs", "Builds", ".git", "PackageCache"]]
            
        for file in files:
            if file.endswith(".prefab"):
                # Skip examples and samples
                if "examples" in root.lower() or "samples" in root.lower():
                    continue

                prefab_path = os.path.join(root, file)
                
                try:
                    with open(prefab_path, 'r', encoding='utf-8') as f:
                        content = f.read()
                        matches = re.finditer(r'm_Script: \{fileID: \d+, guid: ([a-f0-9]{32}), type: \d+\}', content)
                        for match in matches:
                            guid = match.group(1)
                            if guid in guid_map:
                                used_scripts.add(guid_map[guid])
                except Exception as e:
                    print(f"Error reading {prefab_path}: {e}")
    
    return used_scripts

def main():
    print("Analyzing Scripts directory...")
    
    # Get all scripts in the Scripts folder
    all_scripts = get_all_scripts(SCRIPTS_DIR)
    print(f"\nTotal scripts in Assets/Assets/Scripts: {len(all_scripts)}")
    
    # Build GUID mapping
    print("\nBuilding GUID mapping...")
    guid_map = get_script_guids(PROJECT_ROOT)
    
    # Get used scripts in scenes
    print("Scanning scenes...")
    used_in_scenes = get_used_scripts_in_scenes(PROJECT_ROOT, guid_map)
    
    # Get used scripts in prefabs
    print("Scanning prefabs...")
    used_in_prefabs = get_used_scripts_in_prefabs(PROJECT_ROOT, guid_map)
    
    # Combine all used scripts
    all_used_scripts = used_in_scenes | used_in_prefabs
    
    # Find unused scripts
    unused_scripts = all_scripts - all_used_scripts
    
    # Report
    print("\n" + "="*80)
    print("REPORT: Script Usage Analysis")
    print("="*80)
    
    print(f"\nSTATISTICS:")
    print(f"   Total scripts in Scripts folder: {len(all_scripts)}")
    print(f"   Scripts used in scenes: {len(used_in_scenes)}")
    print(f"   Scripts used in prefabs: {len(used_in_prefabs)}")
    print(f"   Total unique scripts in use: {len(all_used_scripts)}")
    print(f"   Unused scripts: {len(unused_scripts)}")
    
    print(f"\nUSED SCRIPTS ({len(all_used_scripts)}):")
    for script in sorted(all_used_scripts):
        in_scene = "[SCENE]" if script in used_in_scenes else ""
        in_prefab = "[PREFAB]" if script in used_in_prefabs else ""
        markers = f"{in_scene}{in_prefab}".strip()
        if markers:
            print(f"   {markers:20} {script}")
        else:
            print(f"   {'':20} {script}")
    
    print(f"\nUNUSED SCRIPTS ({len(unused_scripts)}):")
    for script in sorted(unused_scripts):
        print(f"   {script}")
    
    print("\n" + "="*80)
    print("Legend: [SCENE] = Used in scenes | [PREFAB] = Used in prefabs")
    print("="*80)

if __name__ == "__main__":
    main()
