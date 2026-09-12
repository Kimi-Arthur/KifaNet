#!/usr/bin/env python3
import os
import re
import subprocess
import sys
from collections import defaultdict

def find_unused_usings(target_path=None):
    cmd = [
        "dotnet", "build",
        "-warnAsMessage:CS1591",
        "/p:GenerateDocumentationFile=true",
        "/p:EnforceCodeStyleInBuild=true",
        "--no-incremental"
    ]
    res = subprocess.run(cmd, capture_output=True, text=True)
    pattern = re.compile(r"([^\r\n]+\.cs)\((\d+),\d+\): warning IDE0005: Using directive is unnecessary\.")
    
    file_lines = defaultdict(set)
    target_abs = os.path.abspath(target_path) if target_path else None
    
    for line in res.stdout.splitlines():
        m = pattern.search(line)
        if m:
            file_path = m.group(1).strip()
            line_num = int(m.group(2))
            abs_path = os.path.abspath(file_path) if not os.path.isabs(file_path) else file_path
            
            if target_abs:
                if os.path.isfile(target_abs) and abs_path != target_abs:
                    continue
                if os.path.isdir(target_abs) and not abs_path.startswith(target_abs):
                    continue
            
            file_lines[abs_path].add(line_num)
                
    return file_lines

def remove_unused_lines(file_lines):
    total_removed = 0
    for file_path, lines_to_remove in file_lines.items():
        if not os.path.isfile(file_path):
            continue
        with open(file_path, "r", encoding="utf-8") as f:
            lines = f.readlines()
        
        valid_indices = set()
        for ln in sorted(lines_to_remove, reverse=True):
            idx = ln - 1
            if 0 <= idx < len(lines):
                line_content = lines[idx].strip()
                if line_content.startswith("using ") and line_content.endswith(";"):
                    valid_indices.add(idx)
        
        if valid_indices:
            new_lines = [line for i, line in enumerate(lines) if i not in valid_indices]
            with open(file_path, "w", encoding="utf-8") as f:
                f.writelines(new_lines)
            total_removed += len(valid_indices)
            
    return total_removed

def main():
    target = sys.argv[1] if len(sys.argv) > 1 else None
    pass_num = 1
    total_all_passes = 0
    while True:
        unused = find_unused_usings(target)
        count = sum(len(v) for v in unused.values())
        if count == 0:
            break
        removed = remove_unused_lines(unused)
        total_all_passes += removed
        pass_num += 1
        if pass_num > 5:
            break
    print(f"Cleaned up {total_all_passes} unused using directives.")

if __name__ == "__main__":
    main()
