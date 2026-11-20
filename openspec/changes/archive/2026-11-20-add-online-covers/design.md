# Design

## Context

Covers are files in the covers folder named in `Book.CoverFileName`; `CoverStore` resolves only plain image names
there. No HTTP client exists yet. Decisions marked **(my call)** were made without asking, to keep the roadmap loop
moving.

## Goals / Non-Goals

**Goals:** fill in missing covers by ISBN, politely, only on request.

**Non-Goals:** fetching other metadata, cover choice per book, other cover services, automatic fetching after scans
(my call: the user asks each time; nothing runs in the background).

## Decisions

### 1. `ICoverFetcher.FetchMissingAsync(progress, cancellationToken)`

Loads books with an ISBN (via `MatchKeys.Isbn`) and no `CoverFileName`, then for each: GET
`https://covers.openlibrary.org/b/isbn/{isbn}-L.jpg?default=false`. A 200 response whose body starts like a JPEG or
PNG and is under 5 MB is saved as `ol-{isbn}.jpg`/`.png` in the covers folder and set as the book's cover; 404 means
no cover. Other errors are counted, not thrown. A one-second pause between requests keeps within Open Library's
limits. Returns `CoverFetchResult(Looked, Found, Failed)`; progress reports (done, total).

### 2. HTTP client

A singleton `HttpClient` with a 20-second timeout and a generic `User-Agent: readerCompanion` (no user details).
Tests replace the handler and the pause.

### 3. UI

Settings gains a "Covers" card: the switch (`AppPreferences.OnlineCovers`), and while on, Fetch missing covers with a
progress line and Stop (cancels the token). The result says how many covers were found.
