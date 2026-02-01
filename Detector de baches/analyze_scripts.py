
import os
import glob
import re
# Path to the project root
PROJECT_ROOT = r"g:\Github\Software de simulacion\Recogedor-de-basura\Detector de baches"

def get_script_guids(root_dir):
    guid_map = {}
    print("Scanning for meta files...")
    # Walk to find all .cs.meta files
    for root, dirs, files in os.walk(root_dir):
        # Modify dirs in-place to skip huge folders
        dirs[:] = [d for d in dirs if d not in ["Library", "Temp", "Obj", "Logs", "Builds", ".git", "PackageCache"]]

        for file in files:
            if file.endswith(".cs.meta"):
                full_path = os.path.join(root, file)
                try:
                    with open(full_path, 'r', encoding='utf-8') as f:
                        content = f.read()
                        # Extract GUID
                        match = re.search(r'guid: ([a-f0-9]{32})', content)
                        if match:
                            guid = match.group(1)
                            # Get the script name (remove .meta)
                            script_path = full_path[:-5]
                            script_name = os.path.basename(script_path)
                            guid_map[guid] = script_name
                except Exception as e:
                    print(f"Error reading {full_path}: {e}")
    return guid_map

def analyze_scenes(root_dir, guid_map):
    scene_scripts = {}
    print("Scanning for scene files...")
    
    for root, dirs, files in os.walk(root_dir):
        # Modify dirs in-place to skip huge folders
        dirs[:] = [d for d in dirs if d not in ["Library", "Temp", "Obj", "Logs", "Builds", ".git", "PackageCache"]]
            
        for file in files:
            if file.endswith(".unity"):
                # Exclude specific scenes if needed
                if "prueba" in file.lower():
                    continue
                if "examples" in root.lower() or "samples" in root.lower():
                     continue

                scene_path = os.path.join(root, file)
                scene_name = file
                
                used_scripts = set()
                
                try:
                    with open(scene_path, 'r', encoding='utf-8') as f:
                        content = f.read()
                        # Find all script references
                        # valid reference: m_Script: {fileID: 11500000, guid: <GUID>, type: 3}
                        matches = re.finditer(r'm_Script: \{fileID: \d+, guid: ([a-f0-9]{32}), type: \d+\}', content)
                        for match in matches:
                            guid = match.group(1)
                            if guid in guid_map:
                                used_scripts.add(guid_map[guid])
                except Exception as e:
                    print(f"Error reading {scene_path}: {e}")
                
                if used_scripts:
                    scene_scripts[scene_name] = sorted(list(used_scripts))
    
    return scene_scripts

def main():
    guid_map = get_script_guids(PROJECT_ROOT)
    print(f"Found {len(guid_map)} scripts.")
    
    scene_data = analyze_scenes(PROJECT_ROOT, guid_map)
    
    print("\nXXX_REPORT_START_XXX\n")
    for scene, scripts in scene_data.items():
        print(f"Scene: {scene}")
        for script in scripts:
            print(f"  - {script}")
        print("")
    print("XXX_REPORT_END_XXX")

if __name__ == "__main__":
    main()
