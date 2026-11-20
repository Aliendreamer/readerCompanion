# Proposal

## Why

M8: versions and a changelog derived from the Conventional Commits, and builds that run without installing .NET.

## What Changes

- MinVer sets every assembly's version from git tags (`vX.Y.Z`); untagged builds read `0.1.0-alpha.N`.
- `pnpm release:dry` / `pnpm release` (commit-and-tag-version) write `CHANGELOG.md` and tag locally; nothing is pushed.
- `pnpm package:linux` / `pnpm package:windows` produce self-contained single-file builds in `artifacts/`.
- `docs/releasing.md` explains it. Installers stay an open decision.

## Capabilities

### New Capabilities

None (tooling only; no app behaviour changes).

### Modified Capabilities

None.

## Impact

- `Directory.Build.props`, `Directory.Packages.props` (MinVer 6.0.0, exact), `package.json` (commit-and-tag-version
  13.3.0, exact), `.versionrc.json`, `.gitignore` (`artifacts/`), `docs/releasing.md`.
