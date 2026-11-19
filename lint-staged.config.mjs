import path from "node:path";

// Claude Code's sandbox blocks the socket full `dotnet format` needs, so agent commits get whitespace-only
// formatting; everyone else gets whitespace + style + analyzers. `pnpm format:check` covers the gap.
const inAgentSandbox = process.env.CLAUDECODE === "1";

const relativeArgs = (files) => files.map((f) => JSON.stringify(path.relative(process.cwd(), f))).join(" ");

export default {
  "*.cs": (files) => {
    const authored = files.filter((f) => !/[\\/]Migrations[\\/]/.test(f)); // EF-generated
    if (authored.length === 0) return [];
    const include = relativeArgs(authored);
    return inAgentSandbox
      ? `dotnet format whitespace --folder --include ${include}`
      : `dotnet format ReaderCompanion.slnx --include ${include}`;
  },
  // --no-globs: check only the staged files, not every glob in .markdownlint-cli2.jsonc.
  "*.md": "markdownlint-cli2 --fix --no-globs",
};
