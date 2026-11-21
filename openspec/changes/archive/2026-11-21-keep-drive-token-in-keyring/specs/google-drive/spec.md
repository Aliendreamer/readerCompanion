# Spec Delta

## MODIFIED Requirements

### Requirement: Link Google Drive

The Sources page SHALL show a Google Drive card. When the app has an OAuth client, Link Google Drive SHALL open the
system browser at Google's sign-in for the chosen access level, wait for the answer on a loopback address, and on
success keep the user's token protected and show the linked account and level. The token SHALL be kept in the
system keyring on Linux when one is available, encrypted for the user on Windows, and otherwise in a file in the
data folder readable only by the user; a token found in that file SHALL move into the keyring when it becomes
available. The user SHALL be able to cancel the wait. A refused or failed sign-in SHALL leave Drive unlinked and
say why. When the app has no OAuth client, the card SHALL say that Drive is not set up in this build and offer no
link.

#### Scenario: Link

- **GIVEN** an OAuth client and Drive not linked
- **WHEN** the user presses Link Google Drive and signs in in the browser
- **THEN** the card shows the account and the access level, and the token is kept for the next start

#### Scenario: Token in the keyring

- **GIVEN** Linux with a running keyring and a Drive link stored in the data folder's file by an older version
- **WHEN** the app starts
- **THEN** the link is in the keyring, the file is gone, and Drive is still linked

#### Scenario: No keyring

- **GIVEN** Linux without a keyring service
- **WHEN** the user links Drive
- **THEN** the token is kept in the data folder's file, readable only by the user

#### Scenario: Sign-in refused

- **WHEN** the user denies access in the browser
- **THEN** the card says access was not given and Drive stays unlinked

#### Scenario: No OAuth client

- **GIVEN** a build without `google-oauth-client.json`
- **WHEN** the Sources page is shown
- **THEN** the Drive card says Drive is not set up in this build and has no Link button
