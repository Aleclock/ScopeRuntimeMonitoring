#!/usr/bin/env bash
# generate_unused_candidates.sh
# Run from repository root. Produces files under ./tmp/ for easy access in the project.

set -euo pipefail

ROOT_DIR="$(cd "$(dirname "$0")/.." && pwd)"
OUT_DIR="$ROOT_DIR/tmp"
mkdir -p "$OUT_DIR"

command -v rg >/dev/null 2>&1 || { echo "ripgrep (rg) is required. Install via 'brew install rg'"; exit 1; }

echo "Scanning repository for class definitions..."
rg --hidden --glob '!Library/**' --glob '!.git/**' --pcre2 -o '\bclass\s+([A-Za-z_][A-Za-z0-9_]*)' \
  | sed -E 's/.*class\s+//' | sort -u > "$OUT_DIR/classes.txt"

echo "Finding candidate unused classes (only definition found)..."
: > "$OUT_DIR/unused_candidates.txt"
while read -r c; do
  cnt=$(rg --hidden --glob '!Library/**' --glob '!.git/**' -n --no-ignore -F "$c" | wc -l)
  if [ "$cnt" -le 1 ]; then
    echo "$c ($cnt)" >> "$OUT_DIR/unused_candidates.txt"
    rg -n --hidden --glob '!Library/**' --glob '!.git/**' "class\\s+$c" --pcre2 >> "$OUT_DIR/unused_candidates.txt" || true
    echo "" >> "$OUT_DIR/unused_candidates.txt"
  fi
done < "$OUT_DIR/classes.txt"

echo "Building class -> file -> GUID map..."
: > "$OUT_DIR/class_guid.tsv"
rg --hidden --glob '!Library/**' --glob '!.git/**' --files -g '*.cs' | while read -r f; do
  name=$(rg --pcre2 -o '\b(class|struct)\s+([A-Za-z_][A-Za-z0-9_]*)' "$f" | sed -E 's/.*(class|struct)\s+//' | awk '{print $1}' | head -n1)
  guid=$(sed -n 's/^guid: //p' "$f.meta" 2>/dev/null || true)
  echo -e "${name}\t${f}\t${guid}" >> "$OUT_DIR/class_guid.tsv"
done

echo "Searching scenes/prefabs for GUID references..."
: > "$OUT_DIR/guid_references.txt"
while IFS=$'\t' read -r name file guid; do
  [ -z "$guid" ] && continue
  if rg -n --hidden --glob '!Library/**' --glob '!.git/**' --glob '*.unity' --glob '*.prefab' -F "$guid" >/dev/null; then
    echo -e "${name}\t${guid}" >> "$OUT_DIR/guid_references.txt"
  fi
done < "$OUT_DIR/class_guid.tsv"

echo "Done. Results written to: $OUT_DIR"
echo "- $OUT_DIR/unused_candidates.txt  (candidates to review)"
echo "- $OUT_DIR/class_guid.tsv           (class -> file -> guid mapping)"
echo "- $OUT_DIR/guid_references.txt     (classes referenced in scenes/prefabs by GUID)"

echo "Next steps: open the files in VS Code, review candidates, then move files to an archive folder for safe testing if you want to try removals."
