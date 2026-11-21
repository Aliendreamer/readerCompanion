# Spec Delta

## ADDED Requirements

### Requirement: The OAuth client is built into the program

When `google-oauth-client.json` is present at build time, its client ID and secret SHALL be compiled into the
program in scrambled form, and no file holding them SHALL be copied to the build or publish output. Release builds
SHALL obfuscate the assembly that holds and unscrambles them. The app SHALL unscramble them only in memory and SHALL
never write them to disk. A `google-oauth-client.json` in the data folder SHALL be used instead when present.

#### Scenario: A release build

- **GIVEN** a release build made with `google-oauth-client.json` present
- **WHEN** its output folder is searched for the client ID and the client secret as plain text, in every file
- **THEN** neither is found, and there is no `google-oauth-client.json`
- **AND** the app can link Google Drive

#### Scenario: A build without the client file

- **GIVEN** a build made without `google-oauth-client.json`
- **WHEN** the Sources page is shown
- **THEN** the Drive card says Drive is not set up in this build

#### Scenario: The user's own client

- **GIVEN** a build with a built-in client, and another client's `google-oauth-client.json` in the data folder
- **WHEN** the user links Google Drive
- **THEN** the data folder's client is used
