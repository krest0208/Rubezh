#!/usr/bin/env python3
from pathlib import Path
import hashlib

ROOT = Path(__file__).resolve().parents[1]
parts = sorted((ROOT / "game_parts").glob("part_*.part"))
if not parts:
    raise SystemExit("No game source chunks found")

data = b"".join(p.read_bytes() for p in parts)
expected = "d7df97d2bdf77142b19024c5ca1a28bfdc08369695a3700fd61604957ef1f396"
actual = hashlib.sha256(data).hexdigest()
if actual != expected:
    raise SystemExit(f"SHA256 mismatch: {actual} != {expected}")

out = ROOT / "web" / "index.html"
out.parent.mkdir(parents=True, exist_ok=True)
out.write_bytes(data)

print(f"Restored {out} ({len(data)} bytes)")
print(f"SHA256 {actual}")
