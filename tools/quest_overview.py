"""
Creates QuestOverview.md: all main quests and sub quests with the texts as they appear in the quest log,
in all languages. Also warns about texts which the quest log truncates (see QuestLog.TruncateText).

Usage (Python 3.8+, no dependencies):

    python tools/quest_overview.py [output file]

The default output file is QuestOverview.md in the repository root.
"""

import os
import re
import sys

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
QUESTS = os.path.join(ROOT, "Ambermoon.Core", "UI", "Quests.cs")
QUEST_TEXTS = os.path.join(ROOT, "Ambermoon.Core", "UI", "QuestTexts.cs")

# QuestLog.TruncateText: longer texts are cut and end with ".."
MAX_MAIN_QUEST_LENGTH = 44
MAX_SUB_QUEST_LENGTH = 43  # including the counter, e.g. " (0/6)"

LANGUAGES = [("English", "English"), ("German", "Deutsch"), ("French", "Français"), ("Polish", "Polski"), ("Czech", "Čeština")]


def block_end(lines, start):
    """Index of the closing brace with the same indentation as the line at start."""
    indent = len(lines[start]) - len(lines[start].lstrip())
    return next(i for i in range(start + 1, len(lines)) if lines[i] == " " * indent + "}")


def read_quests():
    lines = open(QUESTS, encoding="utf-8-sig").read().splitlines()

    # the advanced quests are created by a local function, some of them only since a specific episode
    advanced_start = next(i for i, line in enumerate(lines) if "List<MainQuest> CreateAdvancedQuests()" in line)
    advanced_end = block_end(lines, advanced_start)
    episode_blocks = []

    for i, line in enumerate(lines):
        match = re.match(r"\s*if \(episode >= (\d+)\)", line)
        if match:
            episode_blocks.append((i, block_end(lines, i), int(match.group(1))))

    main_quests = []
    current = None

    for i, line in enumerate(lines):
        match = re.search(r"CreateMainQuest\(this, MainQuestType\.(\w+)", line)
        if match:
            episode = next((e for start, end, e in episode_blocks if start < i < end), 1)
            current = {"type": match.group(1), "advanced": advanced_start < i < advanced_end, "episode": episode, "subs": []}
            main_quests.append(current)
            continue
        match = re.search(r"Type = SubQuestType\.(\w+),", line)
        if match and current is not None:
            current["subs"].append([match.group(1), None])
            continue
        match = re.search(r"MinAmount = (\d+)", line)
        if match and current is not None and current["subs"]:
            current["subs"][-1][1] = int(match.group(1))

    # the game appends the advanced quests to the original ones
    main_quests.sort(key=lambda q: q["advanced"])

    return main_quests


def read_texts():
    main_texts, sub_texts = {}, {}
    language = None

    for line in open(QUEST_TEXTS, encoding="utf-8-sig").read().splitlines():
        match = re.search(r"\{\s*GameLanguage\.(\w+)", line)
        if match:
            language = match.group(1)
        match = re.search(r'\{\s*(MainQuestType|SubQuestType)\.(\w+),\s*"([^"]*)"\s*\}', line)
        if match and language:
            table = main_texts if match.group(1) == "MainQuestType" else sub_texts
            table.setdefault(language, {})[match.group(2)] = match.group(3)

    return main_texts, sub_texts


def text(table, language, key):
    # languages without a translation show the english text
    return table.get(language, {}).get(key) or table["English"].get(key) or key


def main():
    output = sys.argv[1] if len(sys.argv) > 1 else os.path.join(ROOT, "QuestOverview.md")
    main_quests = read_quests()
    main_texts, sub_texts = read_texts()
    too_long = []

    out = [
        "# Quest log overview",
        "",
        "All main quests and sub quests with the texts as they appear in the quest log of the remake.",
        "Sub quests with a counter show it like the quest log does at the start, e.g. `(0/6)`.",
        "Languages without a translation of a text show the English text in the game.",
        "",
        "The order is the definition order in `Ambermoon.Core/UI/Quests.cs`, the texts are from",
        "`Ambermoon.Core/UI/QuestTexts.cs`. The quest log itself lists the quests in the order they were activated.",
        "Created with `python tools/quest_overview.py`.",
        "",
        "- " + " | ".join(f"[{native}](#{native.lower()})" for _, native in LANGUAGES),
        "",
    ]

    for language, native in LANGUAGES:
        out += [f"## {native}", ""]
        section = None

        for main_quest in main_quests:
            if not main_quest["advanced"]:
                name = "Original and Ambermoon Advanced"
            elif main_quest["episode"] == 1:
                name = "Ambermoon Advanced only"
            else:
                name = f"Ambermoon Advanced only (since episode {main_quest['episode']})"

            if name != section:
                if section is not None:
                    out.append("")
                section = name
                out += [f"### {name}", ""]

            main_text = text(main_texts, language, main_quest["type"])
            if len(main_text) > MAX_MAIN_QUEST_LENGTH:
                too_long.append((language, main_text, MAX_MAIN_QUEST_LENGTH))
            out.append(f"- **{main_text}**")

            for sub_quest, amount in main_quest["subs"]:
                sub_text = text(sub_texts, language, sub_quest)
                if amount is not None:
                    sub_text += f" (0/{amount})"
                # the counter can have more digits later
                longest = sub_text if amount is None else sub_text.replace(f"(0/{amount})", f"({amount}/{amount})")
                if len(longest) > MAX_SUB_QUEST_LENGTH:
                    too_long.append((language, longest, MAX_SUB_QUEST_LENGTH))
                out.append(f"  - {sub_text}")

        out.append("")

    with open(output, "w", encoding="utf-8", newline="\n") as file:
        file.write("\n".join(out))

    print(f"{output}: {len(main_quests)} main quests, {sum(len(q['subs']) for q in main_quests)} sub quests")

    for language, line, limit in too_long:
        print(f"WARNING {language}: '{line}' has {len(line)} characters, the quest log truncates it after {limit}")

    return 1 if too_long else 0


if __name__ == "__main__":
    sys.exit(main())
