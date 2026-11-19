# Proposal

## Why

The repo has no CI by choice, so nothing enforces formatting, Markdown style, or the Conventional Commits
messages that SemVer versioning depends on. Local git hooks become the quality gate: every commit is
formatted and linted, and badly formed commit messages are rejected before they land on `main`.

## What Changes

- Add a dev-only Node toolchain at the repo root (`package.json`, pnpm, exact version pins): Husky,
  lint-staged, commitlint with the Conventional Commits preset, and markdownlint-cli2.
- **pre-commit hook**: formats staged C# files with `dotnet format` and fixes staged Markdown with
  markdownlint-cli2; the fixes are re-staged, and unfixable Markdown violations block the commit.
- **commit-msg hook**: rejects messages that are not valid Conventional Commits.
- `pnpm` scripts for running the same checks by hand (`lint:md`, `lint:md:fix`, `format`, `format:check`).
- Markdownlint config for the repo's authored Markdown; existing docs brought into compliance.
- Remove the GitHub Actions workflow (already deleted in the working tree; CI/CD is out of scope for this
  project) and update `docs/ROADMAP.md` and `CLAUDE.md` so the documented quality gates point at the
  local hooks and scripts instead of CI.

## Capabilities

### New Capabilities

None — this is developer tooling; the app's behavior does not change (`skip_specs: true`).

### Modified Capabilities

None.

## Impact

- New files: `package.json`, `pnpm-lock.yaml`, `.husky/pre-commit`, `.husky/commit-msg`,
  `lint-staged.config.mjs`, `commitlint.config.mjs`, `.markdownlint-cli2.jsonc`.
- Modified: `.gitignore` (`node_modules/`), `docs/ROADMAP.md`, `CLAUDE.md`, any authored `.md` files the
  linter flags. Removed: `.github/workflows/ci.yml`.
- New prerequisite for committing: Node ≥ 22 and pnpm (both already installed). After cloning, run
  `pnpm install` once to activate the hooks.
- No change to the .NET projects or the app.
