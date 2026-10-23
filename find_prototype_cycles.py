#!/usr/bin/env python3
"""
Detects circular `parent:` inheritance chains across SS14 prototype YAML files.
Run from the repo root: python3 find_prototype_cycles.py
Requires: pip install pyyaml --break-system-packages
"""
import sys
import os
from pathlib import Path

try:
    import yaml
except ImportError:
    print("Missing pyyaml. Install with: pip install pyyaml --break-system-packages")
    sys.exit(1)

# SS14 YAML uses custom tags like !type:ContainerSlot, !type:AllOf, etc.
# yaml.safe_load has no constructor for these and fails the WHOLE FILE on any of them.
# Since we only care about type/id/parent, just ignore the tag and parse the
# underlying node (mapping/sequence/scalar) normally.
class PermissiveLoader(yaml.SafeLoader):
    pass

def _ignore_custom_tag(loader, tag_suffix, node):
    if isinstance(node, yaml.MappingNode):
        return loader.construct_mapping(node, deep=True)
    if isinstance(node, yaml.SequenceNode):
        return loader.construct_sequence(node, deep=True)
    return loader.construct_scalar(node)

PermissiveLoader.add_multi_constructor('', _ignore_custom_tag)

PROTOTYPE_DIR = "Resources/Prototypes"

def load_all_prototypes(root: str):
    """Returns dict: (type, id) -> {parent: str|list|None, file: str}"""
    entries = {}
    for path in Path(root).rglob("*.yml"):
        try:
            with open(path, "r", encoding="utf-8") as f:
                docs = yaml.load(f, Loader=PermissiveLoader)
        except Exception as e:
            print(f"  [skip] Failed to parse {path}: {e}")
            continue

        if not docs:
            continue

        for doc in docs:
            if not isinstance(doc, dict):
                continue
            ptype = doc.get("type")
            pid = doc.get("id")
            if ptype is None or pid is None:
                continue
            parent = doc.get("parent")

            # id can be a single string, or a list of ids sharing one body
            ids = pid if isinstance(pid, list) else [pid]
            for single_id in ids:
                if not isinstance(single_id, str):
                    continue
                entries[(ptype, single_id)] = {
                    "parent": parent,
                    "file": str(path),
                }
    return entries

def find_cycle(entries):
    """DFS cycle detection per prototype kind (type)."""
    WHITE, GRAY, BLACK = 0, 1, 2
    color = {k: WHITE for k in entries}
    cycles_found = []

    def parents_of(key):
        parent = entries[key]["parent"]
        if parent is None:
            return []
        if isinstance(parent, str):
            return [(key[0], parent)]
        if isinstance(parent, list):
            return [(key[0], p) for p in parent]
        return []

    def dfs(key, path):
        color[key] = GRAY
        path.append(key)
        for p_key in parents_of(key):
            if p_key not in entries:
                # parent not found at all - separate issue, not a cycle, skip
                continue
            if color[p_key] == GRAY:
                # found a cycle - report the loop segment
                cycle_start = path.index(p_key)
                cycles_found.append(path[cycle_start:] + [p_key])
            elif color[p_key] == WHITE:
                dfs(p_key, path)
        path.pop()
        color[key] = BLACK

    for key in list(entries.keys()):
        if color[key] == WHITE:
            dfs(key, [])

    return cycles_found

def main():
    if not os.path.isdir(PROTOTYPE_DIR):
        print(f"Can't find {PROTOTYPE_DIR} - run this from your repo root.")
        sys.exit(1)

    print(f"Scanning {PROTOTYPE_DIR} for prototype YAML...")
    entries = load_all_prototypes(PROTOTYPE_DIR)
    print(f"Loaded {len(entries)} prototypes. Checking for parent cycles...\n")

    cycles = find_cycle(entries)

    # Also check for dangling parent references (parent id doesn't exist at all)
    dangling = []
    for key, data in entries.items():
        parent = data["parent"]
        if parent is None:
            continue
        parent_list = parent if isinstance(parent, list) else [parent]
        for p in parent_list:
            if (key[0], p) not in entries:
                dangling.append((key, p, data["file"]))

    if dangling:
        print(f"Found {len(dangling)} dangling parent reference(s) (parent ID doesn't exist):\n")
        for key, missing_parent, file in dangling:
            print(f"  ({key[0]}) {key[1]}  -->  parent: {missing_parent}  [NOT FOUND]   [{file}]")
        print()

    if not cycles and not dangling:
        print("No circular parent inheritance found, and no dangling parent references either.")
        print("The hang is likely something else - worth checking custom prototype")
        print("KIND definitions (IPrototype implementations) for cross-kind cycles instead.")
        return
    elif not cycles:
        return

    print(f"Found {len(cycles)} cycle(s):\n")
    for i, cycle in enumerate(cycles, 1):
        print(f"Cycle {i}:")
        for key in cycle:
            entry = entries.get(key, {})
            print(f"  ({key[0]}) {key[1]}  -->  parent: {entry.get('parent')}   [{entry.get('file')}]")
        print()

if __name__ == "__main__":
    main()
