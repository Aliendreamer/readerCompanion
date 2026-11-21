readerCompanion OAuth website

1. Keep privacy.html in step with the application: Drive access levels and actions, token storage, network connections (Google, Open Library), no telemetry.
2. Publish index.html, privacy.html and style.css using GitHub Pages (e.g. in a docs/ folder or a separate Pages repository).
3. Copy the live HTTPS URLs into Google Auth Platform > Branding.
4. The hosting domain and any Google ownership-verification requirements must be checked in Google Cloud; GitHub Pages is not a guarantee of OAuth verification acceptance.
5. google-oauth-client.json is committed encrypted with git-crypt; without the key its contents are unreadable. For a Desktop-app client Google does not treat the secret as confidential, but keep the key file out of the repository.
