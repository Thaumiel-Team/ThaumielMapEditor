import os
import re
import argparse

def parse_commands(base_dir):
    commands_root = os.path.join(base_dir, "ThaumielMapEditor", "Commands")

    parsed_commands = {
        "Console": [],
        "RemoteAdmin": [],
        "SubCommand": []
    }

    name_pattern = re.compile(r'public\s+(?:override\s+)?string\s+(?:Name|Command)\s*=>\s*"([^"]+)";')
    desc_pattern = re.compile(r'public\s+(?:override\s+)?string\s+Description\s*=>\s*"([^"]+)";')
    alias_pattern = re.compile(r'public\s+(?:override\s+)?string\[\]\s+Aliases\s*=>\s*\[(.*?)\];', re.DOTALL)
    args_pattern = re.compile(r'public\s+(?:override\s+)?string\s+VisibleArgs\s*=>\s*"([^"]*)";')
    perm_pattern = re.compile(r'public\s+(?:override\s+)?string\s+RequiredPermission\s*=>\s*"([^"]+)";')
    subcommand_pattern = re.compile(r'class\s+\w+\s*:\s*(?:[\w.]+\s*,\s*)*SubCommand\b')

    if not os.path.isdir(commands_root):
        print(f"[!] Directory not found, skipping: {commands_root}")
        print(f"(Ensure the base directory '{base_dir}' contains the 'ThaumielMapEditor' folder)")
        return

    for root, _, files in os.walk(commands_root):
        for filename in sorted(files):
            if not filename.endswith(".cs"):
                continue

            filepath = os.path.join(root, filename)
            with open(filepath, 'r', encoding='utf-8') as f:
                content = f.read()

            cmd_type = None
            if subcommand_pattern.search(content):
                cmd_type = "SubCommand"
            elif "[CommandHandler(typeof(GameConsoleCommandHandler))]" in content:
                cmd_type = "Console"
            elif "[CommandHandler(typeof(RemoteAdminCommandHandler))]" in content:
                cmd_type = "RemoteAdmin"
            
            if not cmd_type:
                continue

            name_match = name_pattern.search(content)
            if not name_match:
                continue

            name = name_match.group(1)
            desc_match = desc_pattern.search(content)
            alias_match = alias_pattern.search(content)
            args_match = args_pattern.search(content)
            perm_match = perm_pattern.search(content)

            aliases = ""
            if alias_match:
                raw_aliases = alias_match.group(1)
                aliases = ", ".join([a.strip(' "\' \t\r\n') for a in raw_aliases.split(",") if a.strip(' "\' \t\r\n')])

            parsed_commands[cmd_type].append({
                "name": name,
                "description": desc_match.group(1) if desc_match else "No description provided.",
                "aliases": aliases if aliases else "None",
                "args": args_match.group(1) if args_match else "None",
                "permission": perm_match.group(1) if perm_match else "None"
            })

    for key in parsed_commands:
        parsed_commands[key].sort(key=lambda c: c["name"].lower())

    generate_markdown(parsed_commands, base_dir)

def generate_markdown(commands, base_dir):
    output_file = os.path.join(base_dir, "Commands.md")
    
    with open(output_file, "w", encoding="utf-8") as f:
        f.write("# Thaumiel Map Editor - Commands\n\n")
        f.write("This document outlines all available commands, subcommands, and required permissions for the Thaumiel Map Editor.\n\n")

        f.write("## Remote Admin Commands\n\n")
        if commands["RemoteAdmin"]:
            f.write("| Command | Aliases | Description |\n")
            f.write("| :--- | :--- | :--- |\n")
            for cmd in commands["RemoteAdmin"]:
                raw_aliases = cmd['aliases'].strip()
                fmt_aliases = "None" if not raw_aliases or raw_aliases.lower() == "none" else f"`{raw_aliases}`"
                f.write(f"| `{cmd['name']}` | {fmt_aliases} | {cmd['description']} |\n")
        else:
            f.write("*No Remote Admin commands found.*\n")
        f.write("\n---\n\n")

        f.write("### Admin Subcommands\n\n")
        f.write("> [!IMPORTANT]\n")
        f.write("> These commands are executed via the main Remote Admin command `tme <subcommand>`.\n\n")
        if commands["SubCommand"]:
            f.write("| Subcommand | Aliases | Arguments | Permission | Description |\n")
            f.write("| :--- | :--- | :--- | :--- | :--- |\n")
            for cmd in commands["SubCommand"]:
                raw_aliases = cmd['aliases'].strip()
                fmt_aliases = "None" if not raw_aliases or raw_aliases.lower() == "none" else f"`{raw_aliases}`"
                                
                raw_args = cmd['args'].strip()
                if not raw_args or raw_args.lower() == "none":
                    fmt_args = "None"
                else:
                    args_safe = raw_args.replace("<", "&lt;").replace(">", "&gt;").replace("|", "&#124;")
                    fmt_args = f"<code>{args_safe}</code>"
                
                raw_perms = cmd['permission'].strip()
                fmt_perms = "None" if not raw_perms or raw_perms.lower() == "none" else f"`{raw_perms}`"

                f.write(f"| `{cmd['name']}` | {fmt_aliases} | {fmt_args} | {fmt_perms} | {cmd['description']} |\n")
        else:
            f.write("*No Subcommands found.*\n")
        f.write("\n---\n\n")

        f.write("## Console Commands\n\n")
        if commands["Console"]:
            f.write("| Command | Aliases | Description |\n")
            f.write("| :--- | :--- | :--- |\n")
            for cmd in commands["Console"]:
                raw_aliases = cmd['aliases'].strip()
                fmt_aliases = "None" if not raw_aliases or raw_aliases.lower() == "none" else f"`{raw_aliases}`"
                f.write(f"| `{cmd['name']}` | {fmt_aliases} | {cmd['description']} |\n")
        else:
            f.write("*No Console commands found.*\n")

    print(f"[+] Successfully generated {output_file}!")

if __name__ == "__main__":
    parser = argparse.ArgumentParser(description="Parse Thaumiel Map Editor commands into a Markdown README.")
    parser.add_argument("-b", "--base-dir", type=str, default=".", help="The base directory containing the 'ThaumielMapEditor' repository folder. Defaults to the current directory.")
    args = parser.parse_args()
    parse_commands(args.base_dir)