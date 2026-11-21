# Proposal

## Why

Importing a real Goodreads export (1,432 rows) into a library of 83 books matched only 2 of the 11 books that are in
both. The rest became duplicate "wanted" books, because the title and first author had to be equal after
normalising, and real files differ from Goodreads in ordinary ways:

- subtitles: "Clean Architecture" vs "Clean Architecture: A Craftsman's Guide to Software Structure and Design";
- author forms: "Kleppmann, Martin", "Mario Casciaro & Luciano Mammino", all authors in one name joined by commas
  and "and", "Andrew Hunt" vs "Andy Hunt";
- no real author in the file ("Unknown", a web site name) and junk such as "( PDFDrive.com ).epub" in the title.

## What Changes

- After the existing matches (Goodreads id, ISBN, exact title and first author), a row is compared on its main
  title (before the first colon, without bracketed notes or a file extension) with a book not yet linked to
  Goodreads.
- With one such book: if they share an author surname (authors split on ";", "&", "and", commas; "Surname, Given"
  understood) and their ISBNs do not conflict, the row matches it. Otherwise (no usable author, different authors,
  or conflicting ISBNs) the row is kept for review instead of being added as a wanted book.
- The review list's wording covers both kinds of uncertain match.
- The user's export (`goodreads_library_export.csv`) is kept in the repository encrypted with git-crypt; tests use
  public book data shaped like it, and an opt-in test imports the real file when it is present and decrypted.

## Capabilities

### New Capabilities

### Modified Capabilities

- `goodreads-import`: looser matching on main title and author surnames; uncertain title matches go to review.

## Impact

Core `GoodreadsMatch` (main title, surnames); Data `GoodreadsImport` adds the loose step; UI review wording.
Duplicate finding and scans keep using `MatchKeys` unchanged.
