# Design

## Context

- .NET 10 solution `ReaderCompanion.slnx`; formatting rules live in `.editorconfig`
  (EF migrations are marked `generated_code`).
- No CI (see proposal.md — Why). Node 24.18 and pnpm 12.10 are installed.
- The agent works inside a sandbox that blocks Unix sockets. Full `dotnet format` needs a socket to its
  MSBuild build host and crashes there; `dotnet format whitespace --folder` does not need MSBuild and works.
  Claude Code sets `CLAUDECODE=1` in its shell, which identifies that environment.
- Authored Markdown today: `CLAUDE.md`, `docs/**` (roadmap, M0 spec and plan), `openspec/**`.

## Goals / Non-Goals

**Goals:**

- Every commit has formatted C# and lint-clean Markdown, without manual steps.
- Every commit message is a valid Conventional Commit.
- The same checks can be run by hand with one `pnpm` script each.

**Non-Goals:**

- Running the test suite or a full build in hooks (too slow for every commit; developer-flow runs them).
- Release automation / version calculation (later, see ROADMAP M7).
- Linting files that tools own: `.claude/**` (installed skills, hooks kit), `node_modules/**`.

## Decisions

1. **Node Husky + lint-staged rather than Husky.Net or raw git hooks.** markdownlint-cli2 is a Node tool
   anyway, so one toolchain covers all hooks; commitlint is the reference Conventional Commits checker;
   lint-staged handles "staged files only" and re-staging of fixes. Husky.Net would add a second
   toolchain and only a regex message check; raw hooks would need hand-rolled staging logic and a manual
   `core.hooksPath` per clone.
2. **pnpm, exact pins, `"private": true`.** Matches the user's tooling and the `audit-package-version`
   rule (no `^`/`~`). Pins: husky 9.1.7, lint-staged 17.6.1, @commitlint/cli 21.2.4,
   @commitlint/config-conventional 21.2.4, markdownlint-cli2 0.24.1. `packageManager` set to
   `pnpm@12.10.0`. Hooks are installed by the `prepare` script (`husky`) on `pnpm install`.
3. **C# formatting in pre-commit via a lint-staged function config** (`lint-staged.config.mjs`) so it can
   branch on the environment:
   - normal: `dotnet format ReaderCompanion.slnx --include <staged files>` (whitespace + style + analyzers,
     fixes applied);
   - when `CLAUDECODE=1`: `dotnet format whitespace --folder --include <staged files>`, because full
     format cannot run in the agent sandbox. The full check still runs on the user's commits and via
     `pnpm format:check`.
   Alternative considered: always whitespace-only — rejected, it would silently drop style enforcement for
   the user's commits.
4. **Markdown in pre-commit:** `markdownlint-cli2 --fix` on staged `.md` files; remaining (non-auto-fixable)
   violations fail the commit. Config in `.markdownlint-cli2.jsonc` per the `md-files-audit` baseline:
   all rules on, MD013 at 120 columns with code blocks and tables exempt; globs `CLAUDE.md`, `README.md`,
   `docs/**/*.md`, `openspec/**/*.md`; ignores `node_modules`, `.claude`.
5. **commit-msg:** `commitlint --edit "$1"` with `@commitlint/config-conventional`, no custom rules. The
   preset already accepts the `Co-Authored-By:` trailer used on agent commits.
6. **Gate documentation moves from CI to local:** `CLAUDE.md` gets a short "Quality gates" section listing
   the commands developer-flow should run (`dotnet build`, `dotnet test`, `pnpm format:check`,
   `pnpm lint:md`), since there is no CI config for it to discover them from. The ROADMAP "Gates" row
   is updated likewise. Historical M0 spec/plan documents are left as written.

## Risks / Trade-offs

- [Agent commits only get whitespace formatting] → `pnpm format:check` is part of the documented gates;
  the user's own commits and their verification run apply the full check.
- [`dotnet format` on a cold tree triggers a restore and can take ~10–20 s per commit] → acceptable for a
  solo project; `--include` limits analysis output to staged files.
- [Hooks can be skipped with `--no-verify`] → accepted; this is a convenience gate, not a security control.
- [Fresh clone without `pnpm install` has no hooks] → documented in `CLAUDE.md`.
- [Existing Markdown may fail the new lint rules] → fixed in this change before the hook is relied on.

## Migration Plan

Run `pnpm install` once (activates hooks). Rollback: delete the added files and run
`git config --unset core.hooksPath`.
