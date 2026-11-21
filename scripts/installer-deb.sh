#!/usr/bin/env bash
# Builds artifacts/ReaderCompanion-<version>-amd64.deb from artifacts/linux-x64 (made by pnpm package:linux).
# The app goes to /opt/readercompanion, with a launcher and icons; WebKitGTK is a dependency.
set -euo pipefail
cd "$(dirname "$0")/.."

app=artifacts/linux-x64
[[ -x "$app/ReaderCompanion.Desktop" ]] || { echo "no $app: run pnpm package:linux first" >&2; exit 1; }

# The version MinVer gives builds (from the git tag); Debian wants "~" before a pre-release ("0.2.0~alpha.3").
version=$(dotnet msbuild src/ReaderCompanion.Core -t:MinVer -getProperty:MinVerVersion -m:1 | tail -n 1)
deb_version=${version/-/\~}

stage=$(mktemp -d)
chmod 755 "$stage" # mktemp makes it private; it becomes the package root
trap 'rm -r "$stage"' EXIT

mkdir -p "$stage/opt/readercompanion" "$stage/usr/bin" "$stage/usr/share/applications" "$stage/DEBIAN"
# Symbol files (.pdb) are for debugging, not for users.
cp -a "$app/." "$stage/opt/readercompanion/"
find "$stage/opt/readercompanion" -name '*.pdb' -delete
ln -s /opt/readercompanion/ReaderCompanion.Desktop "$stage/usr/bin/readercompanion"
install -m 644 installer/linux/readercompanion.desktop "$stage/usr/share/applications/"
for size in 16 24 32 48 64 128 256 512; do
  install -D -m 644 "assets/logo/png/readercompanion-$size.png" \
    "$stage/usr/share/icons/hicolor/${size}x${size}/apps/readercompanion.png"
done
install -D -m 644 assets/logo/readercompanion.svg "$stage/usr/share/icons/hicolor/scalable/apps/readercompanion.svg"

cat > "$stage/DEBIAN/control" <<CONTROL
Package: readercompanion
Version: $deb_version
Architecture: amd64
Maintainer: Aliendreamer
Homepage: https://github.com/Aliendreamer/readerCompanion
Depends: libwebkit2gtk-4.1-0
Section: misc
Priority: optional
Installed-Size: $(du -sk "$stage" | cut -f1)
Description: E-book library and reading tracker
 readerCompanion keeps your EPUB and PDF books from local folders and Google
 Drive in one library, tracks what you have read and imports Goodreads.
CONTROL

out="artifacts/ReaderCompanion-$version-amd64.deb"
dpkg-deb --root-owner-group -Zxz --build "$stage" "$out" >/dev/null
echo "$out"
