# Proposal

## Why

Organise offered three layouts, and each produced folders the user did not want: "No series", "Unknown genre",
"Unknown author", and authors renamed "Last, First" (moving files only to reorder a name). Programming books the user
keeps together were scattered among fiction authors. The user wants one consistent way, keeping an author together.

## What Changes

- One layout: `Group / Author / [Series /] [NN - ]Title`.
- The group is decided per author, not per book: the genre most of the author's books have (ties: alphabetical);
  for an author whose books have no genre, the top folder their files mostly sit in when it holds several authors' books
  (so `ProgramingBooks` or `qabooks` stay groups), and no group level otherwise (a book's or an author's own folder).
- The author is the first person of the first author, written given name first ("Iain M. Banks", "Martin Kleppmann").
- A series folder only when the book has a series; "NN - " before the title only with a series number.
- Books without a usable author (none, "Unknown", a web address) are not moved.
- The layout choice disappears from the Organise page.

## Capabilities

### New Capabilities

### Modified Capabilities

- `organise-files`: one layout with author groups.

## Impact

Core `OrganisePlanner` (no `OrganiseLayout`), Data `Organiser`, UI `OrganisePage`; their tests.
