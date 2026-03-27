#!/bin/bash
set -e

PROJ="src/OnAirAlert/OnAirAlert.csproj"
OUT="dist"

echo "=== OnAirAlert Build ==="

rm -rf "$OUT"

dotnet publish "$PROJ" \
  -c Release \
  -r win-x64 \
  --self-contained \
  -p:PublishSingleFile=true \
  -p:IncludeNativeLibrariesForSelfExtract=true \
  -o "$OUT"

# Create assets folder
mkdir -p "$OUT/assets"
echo "BGM ファイル (mp3/wav) をここに置いてください" > "$OUT/assets/README.txt"

echo ""
echo "=== Build complete ==="
echo "Output: $OUT/OnAirAlert.exe"
ls -lh "$OUT/OnAirAlert.exe"
