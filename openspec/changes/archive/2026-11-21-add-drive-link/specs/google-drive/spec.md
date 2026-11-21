# Spec Delta

## Purpose

Lets anyone who installs the app link their own Google Drive from the Sources page, at an access level they
choose, so Drive books can later appear and be managed beside local ones.

## ADDED Requirements

### Requirement: Drive access level

The Settings page SHALL offer the Google Drive access level, remembered: only books the user picks (the default), the
whole Drive read only, or the whole Drive read and write. A change SHALL take effect the next time Drive is linked,
and while linked at another level the page SHALL say so.

#### Scenario: Change while linked

- **GIVEN** Drive linked with only picked books
- **WHEN** the user chooses the whole Drive, read and write
- **THEN** the choice is remembered and the page says to link Drive again to use it

### Requirement: Link Google Drive

The Sources page SHALL show a Google Drive card. When the app has an OAuth client, Link Google Drive SHALL open the
system browser at Google's sign-in for the chosen access level, wait for the answer on a loopback address, and on
success store the user's token in the data folder (readable only by the user) and show the linked account and
level. The user SHALL be able to cancel the wait. A refused or failed sign-in SHALL leave Drive unlinked and say why.
When the app has no OAuth client, the card SHALL say that Drive is not set up in this build and offer no link.

#### Scenario: Link

- **GIVEN** an OAuth client and Drive not linked
- **WHEN** the user presses Link Google Drive and signs in in the browser
- **THEN** the card shows the account and the access level, and the token is kept for the next start

#### Scenario: Sign-in refused

- **WHEN** the user denies access in the browser
- **THEN** the card says access was not given and Drive stays unlinked

#### Scenario: No OAuth client

- **GIVEN** a build without `google-oauth-client.json`
- **WHEN** the Sources page is shown
- **THEN** the Drive card says Drive is not set up in this build and has no Link button

### Requirement: Unlink Google Drive

Unlink SHALL revoke the token at Google when it can, and SHALL always delete the stored token, leaving the library
unchanged. If Google later rejects the stored token (revoked or expired), the app SHALL treat Drive as unlinked and
say that it needs linking again.

#### Scenario: Unlink

- **GIVEN** Drive linked
- **WHEN** the user presses Unlink
- **THEN** the token is revoked and deleted, and the card offers Link Google Drive again
