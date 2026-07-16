#!/usr/bin/env python3
from pathlib import Path
root = Path(__file__).resolve().parents[2]
asm = list((root / "Packages").rglob("*.asmdef"))
out = root / "BOOT-BRIEF.generated.md"
lines = ["# BOOT-BRIEF (generated)", "", f"asmdefs: {len(asm)}", ""]
for a in sorted(asm):
    lines.append(f"- `{a.relative_to(root)}`")
out.write_text("\n".join(lines) + "\n", encoding="utf-8")
print("wrote", out)
