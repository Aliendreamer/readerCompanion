# Tasks

## 1. Node toolchain

- [x] 1.1 Add root `package.json` (`private`, `packageManager: pnpm@12.10.0`, exact-pinned devDependencies
  from design.md, scripts `prepare`, `lint:md`, `lint:md:fix`, `format`, `format:check`) and add
  `node_modules/` to `.gitignore`; verify `pnpm install` succeeds, `pnpm-lock.yaml` is created, and
  `git config core.hooksPath` prints `.husky/_`
- [x] 1.2 Verify no dependency uses a range: `jq '.devDependencies' package.json` shows only exact versions

## 2. Markdown linting

- [x] 2.1 Add `.markdownlint-cli2.jsonc` per design.md decision 4; verify `pnpm lint:md` runs and lists
  only files under `CLAUDE.md`, `docs/`, `openspec/`
- [x] 2.2 Bring existing authored Markdown into compliance (`pnpm lint:md:fix`, then fix the rest by hand
  without changing meaning); verify `pnpm lint:md` reports `0 error(s)`

## 3. Commit message check

- [x] 3.1 Add `commitlint.config.mjs` extending `@commitlint/config-conventional` and `.husky/commit-msg`
  running `pnpm exec commitlint --edit "$1"`; verify `echo "bad message" | pnpm exec commitlint` fails and
  `printf 'feat: add x\n\nCo-Authored-By: A <a@b.c>\n' | pnpm exec commitlint` passes

## 4. Pre-commit formatting

- [x] 4.1 Add `lint-staged.config.mjs` (C# per design.md decision 3 incl. the `CLAUDECODE=1` branch;
  Markdown via `markdownlint-cli2 --fix`) and `.husky/pre-commit` running `pnpm exec lint-staged`;
  verify by staging a `.cs` file with broken indentation and a `.md` file with trailing spaces and
  running `pnpm exec lint-staged`: both are fixed and re-staged (then unstage/revert the test edits)
- [x] 4.2 Verify an unfixable Markdown violation (e.g. a code fence without a language) makes
  `pnpm exec lint-staged` exit non-zero, then revert the test edit

## 5. Docs and CI removal

- [x] 5.1 Remove `.github/workflows/ci.yml`; update `docs/ROADMAP.md` (Gates row → local hooks and
  scripts) and add a "Quality gates" section to `CLAUDE.md` (outside the setup-flow markers) listing
  `dotnet build`, `dotnet test`, `pnpm format:check`, `pnpm lint:md`, and the one-time `pnpm install`;
  verify `pnpm lint:md` still reports `0 error(s)`

## 6. Integration check

- [x] 6.1 Commit this change through the real hooks with a Conventional Commit message and verify both
  hooks ran (pre-commit output from lint-staged, commit accepted); verify a commit attempt with the
  message `update stuff` is rejected by commit-msg (use an empty staged change or `--allow-empty`)

## Workflow follow-up

- User runs `pnpm format:check` and a real commit outside the sandbox (full `dotnet format` path).
- Archive the change after the user's approval and manual verification.
