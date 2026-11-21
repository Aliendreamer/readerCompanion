#!/usr/bin/env python3
"""Checks that packages do not give away the app's Google OAuth client.

Fails when any file under the given folders is named like the client file, or holds the client ID or secret as
plain text (UTF-8 or UTF-16, as .NET stores strings).

Usage: python3 scripts/check-client-hidden.py [--client google-oauth-client.json] artifacts
"""
import argparse
import json
import os
import sys

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

found = []
for folder in args.folders:
    for root, _, files in os.walk(folder):
        for name in files:
            path = os.path.join(root, name)
            if "oauth" in name.lower():
                found.append(f"{path}: client file shipped")
                continue
            with open(path, "rb") as f:
                data = f.read()
            if any(n in data for n in needles):
                found.append(f"{path}: holds the client ID or secret as plain text")

if found:
    print("\n".join(found))
    sys.exit(1)
print(f"ok: no client file and no plain client ID or secret in {', '.join(args.folders)}")
