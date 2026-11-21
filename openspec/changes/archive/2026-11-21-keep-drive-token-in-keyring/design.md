# Design

## libsecret through P/Invoke

`libsecret-1.so.0` is installed wherever the app runs on Linux (WebKitGTK depends on it). The varargs functions
cannot be called from .NET, so the `v` variants are used: `secret_password_storev_sync`,
`secret_password_lookupv_sync`, `secret_password_clearv_sync`, with a `GHashTable` of attributes
(`g_hash_table_new(g_str_hash, g_str_equal)`) and a `SecretSchema` laid out by hand (name, flags, 32 attribute
slots, reserved fields: 592 bytes on 64-bit, zeroed). The schema is `org.readercompanion.DriveLink` with one string
attribute, `store`: the token file's path, so separate data folders keep separate links.

## Unavailable keyring

Any `GError`, a missing library or entry point, or no session bus (`DBUS_SESSION_BUS_ADDRESS` unset and no
`$XDG_RUNTIME_DIR/bus`) means "unavailable": `DriveTokenFile` then uses the file as before. The check avoids
D-Bus autolaunch on machines without a session.

## Order

- Save: keyring first; when it accepts the link, any old file is deleted. Otherwise the file is written.
- Load: keyring first; if it has nothing but the file has a link, the link moves into the keyring (file deleted
  once stored). If the keyring is unavailable, the file is read.
- Delete: both.

`IKeyring` is faked in unit tests; `LibsecretKeyring` has one environment test that either round-trips a secret
or reports the keyring unavailable without throwing.
