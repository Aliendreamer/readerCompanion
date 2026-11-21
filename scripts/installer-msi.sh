#!/usr/bin/env bash
# Builds artifacts/ReaderCompanion-<version>-win-x64.msi from artifacts/win-x64 (made by pnpm package:windows).
# WiX only runs on Windows, so from WSL this calls Windows' dotnet.exe (WSL runs Windows programs); paths are handed
# over in Windows form. WiX is pinned in dotnet-tools.json.
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
"$windows_dotnet" tool restore >/dev/null
"$windows_dotnet" tool run wix build "$(wslpath -w installer/windows/readerCompanion.wxs)" -arch x64 \
  -d "Version=$msi_version" -bindpath "App=$(wslpath -w "$app")" -o "$(wslpath -w "$out")"
rm -f "${out%.msi}.wixpdb" # WiX's build record, not for users
echo "$out"
