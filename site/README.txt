readerCompanion OAuth website

1. Review privacy.html against the real application's behavior, especially token storage, Google Drive access, any analytics/telemetry, and external network requests.
2. Publish index.html, privacy.html and style.css using GitHub Pages (e.g. in a docs/ folder or a separate Pages repository).
3. Copy the live HTTPS URLs into Google Auth Platform > Branding.
4. The hosting domain and any Google ownership-verification requirements must be checked in Google Cloud; GitHub Pages is not a guarantee of OAuth verification acceptance.
5. Important: the public repository currently lists google-oauth-client.json. Check whether it contains actual OAuth client credentials; remove it from tracking and rotate if necessary.
