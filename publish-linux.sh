#!/usr/bin/env bash
# Publish Linux x64 single-file self-contained binary.
# Output: publish/linux-x64/Habbo Downloader (+ Tools/ffdec/ kept as Content)

set -euo pipefail
SCRIPT_DIR="$(cd "$(dirname "$0")" && pwd)"
CONFIG=Release
RID=linux-x64
OUT="$SCRIPT_DIR/publish/$RID"

rm -rf "$OUT"

cd "$SCRIPT_DIR/SourceCode"
dotnet publish "Habbo Downloader.csproj" \
    -c "$CONFIG" \
    -r "$RID" \
    --self-contained true \
    -p:PublishSingleFile=true \
    -p:EnableCompressionInSingleFile=true \
    -p:DebugType=none \
    -p:DebugSymbols=false \
    -o "$OUT"

# Mark the binary executable.
chmod +x "$OUT/Habbo Downloader" || true

# Drop a .desktop entry next to the binary so file managers can launch it as GUI.
cat > "$OUT/all-in-1-converter.desktop" <<EOF
[Desktop Entry]
Type=Application
Name=All-in-1 Converter
Comment=Habbo asset workstation (Mainframe / Matrix GUI)
Exec=$OUT/Habbo\ Downloader --gui
Terminal=false
Categories=Development;Utility;
EOF

echo
echo "Published Linux build at: $OUT"
echo "Run from terminal:    $OUT/Habbo\\ Downloader        (prompts TUI / CLI)"
echo "Run from terminal:    $OUT/Habbo\\ Downloader --tui  (mainframe TUI)"
echo "Run from terminal:    $OUT/Habbo\\ Downloader --cli  (plain console, mainframe look)"
echo "Run from file manager (double-click .desktop):  launches GUI directly"
