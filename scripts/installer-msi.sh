#!/usr/bin/env bash
# Builds artifacts/ReaderCompanion-<version>-win-x64.msi from artifacts/win-x64 (made by pnpm package:windows).
# WiX only runs on Windows, so from WSL this calls Windows' dotnet.exe (WSL runs Windows programs); it builds in a
# Windows temp folder (see below). WiX is pinned in dotnet-tools.json.
set -euo pipefail
cd "$(dirname "$0")/.."

app=artifacts/win-x64
[[ -f "$app/ReaderCompanion.Desktop.exe" ]] || { echo "no $app: run pnpm package:windows first" >&2; exit 1; }

windows_dotnet=$(command -v dotnet.exe || echo "/mnt/c/Program Files/dotnet/dotnet.exe")
[[ -x "$windows_dotnet" ]] || { echo "needs Windows' .NET (dotnet.exe), reached from WSL" >&2; exit 1; }

# MinVer's version for the file name; an MSI version must be plain numbers, so the MSI gets major.minor.patch.
read -r version msi_version < <(dotnet msbuild src/ReaderCompanion.Core -t:MinVer -m:1 \
  -getProperty:MinVerVersion -getProperty:MinVerMajor -getProperty:MinVerMinor -getProperty:MinVerPatch |
  python3 -I -c 'import json,sys; p=json.load(sys.stdin)["Properties"]; print(p["MinVerVersion"], ".".join(p[k] for k in ("MinVerMajor","MinVerMinor","MinVerPatch")))')

out="artifacts/ReaderCompanion-$version-win-x64.msi"

# Windows does not trust files it reads through \\wsl.localhost (it marks them as from another computer and blocks
# the tool restore), so WiX works in a folder on the Windows side: the tool manifest, the installer source, the icon
# and the package are copied there, and the MSI is copied back. WiX gets full Windows paths: it does not resolve
# relative ones from the source file's folder.
windows_temp=$(wslpath "$(cmd.exe /c 'echo %TEMP%' 2>/dev/null | tr -d '\r')")
work=$(mktemp -d -p "$windows_temp" readercompanion-msi.XXXXXX)
trap 'rm -r "$work"' EXIT
cp dotnet-tools.json "$work/"
# Its own package source: a fresh Windows .NET may have none configured ("No NuGet sources are defined").
cat >"$work/nuget.config" <<'XML'
<?xml version="1.0" encoding="utf-8"?>
<configuration>
  <packageSources>
    <clear />
    <add key="nuget.org" value="https://api.nuget.org/v3/index.json" />
  </packageSources>
</configuration>
XML
cp installer/windows/readerCompanion.wxs assets/logo/readercompanion.ico "$work/"
cp -r "$app" "$work/app"

(
  cd "$work"
  "$windows_dotnet" tool restore >/dev/null
  "$windows_dotnet" tool run wix build readerCompanion.wxs -arch x64 -d "Version=$msi_version" \
    -d "IconFile=$(wslpath -w "$work/readercompanion.ico")" -bindpath "App=$(wslpath -w "$work/app")" \
    -o ReaderCompanion.msi
)
cp "$work/ReaderCompanion.msi" "$out" # WiX's build record (.wixpdb) stays behind and goes with the folder
echo "$out"
