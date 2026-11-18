# readerCompanion

## Git conventions

- **Work directly on `main`.** No feature branches.
- **Commit messages follow [Conventional Commits](https://www.conventionalcommits.org/)**
  (`feat:`, `fix:`, `chore:`, `docs:`, `test:`, `refactor:`, `ci:`…; `!` or a `BREAKING CHANGE:` footer
  for breaking changes). Versions follow SemVer and are derived from these commits.
- Stage files by explicit path; never `git add -A` (the agent sandbox shows placeholder dotfiles in the repo root).

<!-- setup-flow:start -->
## MANDATORY workflow

**For ANY feature, change, or bugfix you MUST follow the `developer-flow` skill.** Invoke it at the
start of implementation work; do not skip or reorder its steps: brainstorm → plan/proposal →
implement (TDD) → simplify → code review → run the repo's quality gates → report → user approval and
manual verification before archive/commit. If a change adds or modifies a web endpoint, create or
update its `.http` file as part of the same change.

**Prefer semantic code tools for code search and edits** — e.g. Serena MCP or your editor's LSP
(`find_symbol`, `replace_symbol_body`, `find_referencing_symbols`) — over raw text/grep where a
semantic tool applies.

**After finishing a task, optimize skills when there's concrete feedback for it.** If the session
surfaced specific learnings about how a skill performed — an activation gap, a regression, missing or
misleading guidance, or a confirmed improvement — refine that skill (use a skill-optimizing skill if
one is available) before moving on. Only when the feedback is specific; skip it otherwise.
<!-- setup-flow:end -->
