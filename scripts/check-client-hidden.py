#!/usr/bin/env python3
"""Checks that packages do not give away the app's Google OAuth client.

Fails when any file under the given folders is named like the client file, or holds the client ID or secret as
plain text (UTF-8 or UTF-16, as .NET stores strings). Installers are compressed, so they are unpacked and searched
too: .deb (dpkg-deb), .AppImage (its own --appimage-extract) and .msi (msitools' msiextract).

Usage: python3 scripts/check-client-hidden.py [--client google-oauth-client.json] artifacts
"""
import argparse
import json
import os
import shutil
import subprocess
import sys
import tempfile

parser = argparse.ArgumentParser()
parser.add_argument("--client", default="google-oauth-client.json")
parser.add_argument("folders", nargs="+")
args = parser.parse_args()

try:
    with open(args.client, encoding="utf-8") as f:
        installed = json.load(f)["installed"]
except (OSError, ValueError, KeyError) as e:
    sys.exit(f"cannot read the client from {args.client} ({e}); is the repository unlocked with git-crypt?")

values = [installed["client_id"], installed["client_secret"]]
needles = [v.encode(enc) for v in values for enc in ("utf-8", "utf-16-le")]



def unpack(installer, into):
    """Unpacks an installer into the folder `into`; False when it is not an installer."""
    lower = installer.lower()
    if lower.endswith(".deb"):
        subprocess.run(["dpkg-deb", "-x", installer, into], check=True)
    elif lower.endswith(".appimage"):
        subprocess.run([os.path.abspath(installer), "--appimage-extract"], cwd=into, check=True,
                       stdout=subprocess.DEVNULL, env={**os.environ, "APPIMAGE_EXTRACT_AND_RUN": "1"})
    elif lower.endswith(".msi"):
        # msitools' msiextract: WiX can open an MSI only on Windows.
        if shutil.which("msiextract") is None:
            sys.exit("checking an .msi needs msiextract: sudo apt install msitools")
        subprocess.run(["msiextract", "-C", into, installer], check=True, stdout=subprocess.DEVNULL)
    else:
        return False
    return True


def search(folder, shown_as, found):
    for root, _, files in os.walk(folder):
        for name in files:
            path = os.path.join(root, name)
            label = os.path.join(shown_as, os.path.relpath(path, folder))
            if "oauth" in name.lower():
                found.append(f"{label}: client file shipped")
                continue
            if os.path.islink(path):
                continue
            with open(path, "rb") as f:
                data = f.read()
            if any(n in data for n in needles):
                found.append(f"{label}: holds the client ID or secret as plain text")
            with tempfile.TemporaryDirectory() as unpacked:
                if unpack(path, unpacked):
                    search(unpacked, label + "!", found)


found = []
for folder in args.folders:
    search(folder, folder, found)

if found:
    print("\n".join(found))
    sys.exit(1)
print(f"ok: no client file and no plain client ID or secret in {', '.join(args.folders)}")
