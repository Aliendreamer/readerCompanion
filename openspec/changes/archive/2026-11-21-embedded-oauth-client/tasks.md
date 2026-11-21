# Tasks

## 1. Built-in client

- [x] 1.1 `ReaderCompanion.Client`: a build step turns `google-oauth-client.json` into generated source holding the
  ID and secret scrambled; `BuiltInClient.TryGet()` unscrambles them in memory — tests: round trip, no file gives null
- [x] 1.2 `GoogleClientConfig`: data-folder file first, then the built-in client — Data tests
- [x] 1.3 The Desktop project stops copying the JSON to build and publish output

## 2. Obfuscation

- [x] 2.1 Obfuscar (exact version pinned) runs on `ReaderCompanion.Client` in Release builds; `obfuscar.xml`
- [x] 2.2 `pnpm package:check` (`scripts/check-client-hidden.py`): a package holds no client file and no plain
  client ID or secret; checked here with a made-up client, the user runs it on real packages (the sandbox cannot
  read the client file)

## 3. Docs

- [x] 3.1 `RELEASING.md`: how the client gets into builds, and the limits
