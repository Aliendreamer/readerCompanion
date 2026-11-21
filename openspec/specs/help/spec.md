# help Specification

## Purpose

The in-app Help page: step-by-step guides for adding books, linking Google Drive, importing from Goodreads and
changing the text size.

## Requirements

### Requirement: Help page

The sidebar SHALL link to a Help page that explains, step by step, how to add a books folder, how to link Google
Drive (the access levels, the browser sign-in and Google's "unverified app" warning) and how to download the
Goodreads export file and import it, and how to make text larger. The page SHALL list its topics as links at the
top.

#### Scenario: Goodreads steps

- **WHEN** the user opens Help and follows the Goodreads topic link
- **THEN** they see how to export the library on goodreads.com, which `.csv` file to download, and which button
  imports it
