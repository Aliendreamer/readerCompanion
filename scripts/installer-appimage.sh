#!/usr/bin/env bash
# Builds artifacts/ReaderCompanion-<version>-x86_64.AppImage from artifacts/linux-x64 (made by pnpm package:linux).
# One file that runs on any distribution with WebKitGTK installed (WebKitGTK itself is not bundled: it is large and
# tied to the system's graphics stack).
set -euo pipefail
cd "$(dirname "$0")/.."

app=artifacts/linux-x64
[[ -x "$app/ReaderCompanion.Desktop" ]] || { echo "no $app: run pnpm package:linux first" >&2; exit 1; }

# Pinned tools, downloaded once and checked against these checksums (computed when pinned; the releases publish none).
tools="${XDG_CACHE_HOME:-$HOME/.cache}/readercompanion-tools"
appimagetool="$tools/appimagetool-1.9.1-x86_64.AppImage"
appimagetool_url=https://github.com/AppImage/appimagetool/releases/download/1.9.1/appimagetool-x86_64.AppImage
appimagetool_sha=ed4ce84f0d9caff66f50bcca6ff6f35aae54ce8135408b3fa33abfc3cb384eb0
runtime="$tools/runtime-20251108-x86_64"
runtime_url=https://github.com/AppImage/type2-runtime/releases/download/20251108/runtime-x86_64
runtime_sha=2fca8b443c92510f1483a883f60061ad09b46b978b2631c807cd873a47ec260d

fetch() { # file url sha256
  if [[ ! -f "$1" ]]; then mkdir -p "$tools"; curl -fsSL -o "$1.part" "$2"; mv "$1.part" "$1"; fi
  echo "$3  $1" | sha256sum --check --quiet || { echo "checksum mismatch: $1" >&2; rm -f "$1"; exit 1; }
}
fetch "$appimagetool" "$appimagetool_url" "$appimagetool_sha"
fetch "$runtime" "$runtime_url" "$runtime_sha"
chmod +x "$appimagetool"

version=$(dotnet msbuild src/ReaderCompanion.Core -t:MinVer -getProperty:MinVerVersion -m:1 | tail -n 1)

appdir=$(mktemp -d)
trap 'rm -r "$appdir"' EXIT
mkdir -p "$appdir/usr/lib/readercompanion"
cp -a "$app/." "$appdir/usr/lib/readercompanion/"
find "$appdir/usr/lib/readercompanion" -name '*.pdb' -delete # symbols are for debugging, not for users
sed 's|^Exec=.*|Exec=readercompanion|' installer/linux/readercompanion.desktop > "$appdir/readercompanion.desktop"
cp assets/logo/png/readercompanion-256.png "$appdir/readercompanion.png"
cat > "$appdir/AppRun" <<'APPRUN'
#!/bin/sh
exec "$APPDIR/usr/lib/readercompanion/ReaderCompanion.Desktop" "$@"
APPRUN
chmod +x "$appdir/AppRun"

out="artifacts/ReaderCompanion-$version-x86_64.AppImage"
# Extract-and-run: works without FUSE (containers, WSL).
APPIMAGE_EXTRACT_AND_RUN=1 ARCH=x86_64 "$appimagetool" --no-appstream --runtime-file "$runtime" "$appdir" "$out" >/dev/null 2>&1 \
  || { APPIMAGE_EXTRACT_AND_RUN=1 ARCH=x86_64 "$appimagetool" --no-appstream --runtime-file "$runtime" "$appdir" "$out"; exit 1; }
echo "$out"
