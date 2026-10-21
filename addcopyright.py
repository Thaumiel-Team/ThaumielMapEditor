import os
import re

NOTICE_TEMPLATE = """\
// -----------------------------------------------------------------------
// <copyright file="{filename}" company="Thaumiel Team">
// Copyright (c) Thaumiel Team. All rights reserved.
// Licensed under the GNU General Public License v3.0 (GPL-3.0).
// </copyright>
// -----------------------------------------------------------------------
"""

EXCLUDED_DIRS = {"obj"}
EXCLUDED_FILES = {"AssemblyInfo.cs"}

IGNORE_PATTERN = re.compile(r"\[.*PythonIgnore(Attribute)?.*\]")

COPYRIGHT_FILE_PATTERN = re.compile(r'(<copyright\b[^>]*?\bfile=")([^"]*)(")', re.IGNORECASE)

def should_ignore_file(content: str) -> bool:
    return bool(IGNORE_PATTERN.search(content))

def already_has_notice(content: str) -> bool:
    return "<copyright" in content

def update_notice_filename(content: str, filename: str) -> str:
    return COPYRIGHT_FILE_PATTERN.sub(lambda match: f"{match.group(1)}{filename}{match.group(3)}", content, count=1)

def add_notice(file_path: str) -> None:
    filename = os.path.basename(file_path)
    
    with open(file_path, "r", encoding="utf-8") as f:
        content = f.read()

    if should_ignore_file(content):
        print(f"[IGNORED]  Found [PythonIgnore]: {file_path}")
        return

    if already_has_notice(content):
        updated_content = update_notice_filename(content, filename)

        if updated_content == content:
            print(f"[SKIPPED]  Already has notice: {file_path}")
            return

        with open(file_path, "w", encoding="utf-8") as f:
            f.write(updated_content)

        print(f"[UPDATED]  Copyright file name set to '{filename}': {file_path}")
        return

    notice = NOTICE_TEMPLATE.format(filename=filename)
    with open(file_path, "w", encoding="utf-8") as f:
        f.write(notice + "\n" + content)

    print(f"[ADDED]    Notice added to: {file_path}")

def process_directory(root_dir: str) -> None:
    for dirpath, dirnames, filenames in os.walk(root_dir):
        dirnames[:] = [d for d in dirnames if d not in EXCLUDED_DIRS]

        for filename in filenames:
            if not filename.endswith(".cs"):
                continue

            if filename in EXCLUDED_FILES:
                print(f"[SKIPPED]  Excluded file: {filename}")
                continue

            file_path = os.path.join(dirpath, filename)
            add_notice(file_path)

if __name__ == "__main__":
    import sys

    if len(sys.argv) < 2:
        print("Usage: python addcopyright.py <directory>")
        sys.exit(1)

    target_dir = sys.argv[1]

    if not os.path.isdir(target_dir):
        print(f"Error: '{target_dir}' is not a valid directory.")
        sys.exit(1)

    print(f"Processing directory: {target_dir}\n")
    process_directory(target_dir)
    print("\nDone.")