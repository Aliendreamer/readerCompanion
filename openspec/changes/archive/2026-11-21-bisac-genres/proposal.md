# Proposal

## Why

Genres came straight from each EPUB's subject tags: a real library had 562 of them ("Fiction", "General", "Fantasy
Fiction", "SciFi", series names, places). They were useless for filtering and made Organise's author groups wrong.

## What Changes

- A fixed list of genres from BISAC's main headings (the publishing standard): Fantasy, Science Fiction, Horror,
  Mystery & Crime, Thriller, Romance, Historical Fiction, Action & Adventure, War & Military, Literary Fiction, Humour,
  Short Stories, Classics, Young Adult, Programming & Computers, Business, History, Biography & Memoir, Science,
  Philosophy, Psychology, Self-Help, Religion & Spirituality, Travel, Cooking, Art, Poetry, Comics & Graphic Novels.
- File tags are mapped onto it by words ("SciFi", "Fiction - Science Fiction" → Science Fiction) and BISAC codes
  (`FIC028000` → Science Fiction); tags that are not genres are dropped. A book can have several genres.
- Existing books get the same mapping at start (a no-op once clean); books the user edited keep their own genres.
- The filter tree starts with every section closed.
- The "Owned / wanted" filter goes; Status shows a count per status, and "Want to read" how many have files.
- Goodreads titles in two series ("Discworld, #8; City Watch, #1") take the first series; a re-import fixes wanted
  books read wrongly before and removes the broken collections.
- Release-tagged series names from files ("Serified.Dresden Files.SciFan.(#01 of 14)._.ICA Serified") become the
  series they name, numbered from the name.

## Capabilities

### New Capabilities

### Modified Capabilities

- `ebook-metadata`: genres are mapped onto the fixed list.
- `library-browsing`: filter sections start closed; status counts replace the owned/wanted filter.
- `goodreads-import`: two-series titles.

## Impact

Core `GenreMap`; Data `CatalogWriter` (new scans), `CatalogCleanup` (existing books, at start); Core `SeriesName`;
UI `FilterTreeState`, `FilterTree`, `FilterSection`, `LibraryBrowser`; Core `GoodreadsCsv`; Data `GoodreadsImport`.
