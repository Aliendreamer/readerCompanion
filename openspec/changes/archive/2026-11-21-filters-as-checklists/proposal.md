# Proposal

## Why

Picked filter values appeared as chips outside their dropdown, which was confusing: the user expects the choices
to stay inside the dropdown.

## What Changes

- Each filter is a button (with a count of picked values) that opens a checklist; picked values stay in it, ticked.
- Several values can be ticked without reopening; Clear unticks all; Escape or a click outside closes it.
- Lists of 8 or more values get a search box.

## Capabilities

### New Capabilities

### Modified Capabilities

- `library-browsing`: filters are checklists in a dropdown.

## Impact

`FilterPicker.razor` and its styles; browse and panel tests.
