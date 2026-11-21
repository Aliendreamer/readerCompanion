using System.Runtime.InteropServices;

namespace ReaderCompanion.Data;

/// <summary>The Linux keyring through libsecret (Secret Service: GNOME Keyring, KWallet). libsecret ships with
/// WebKitGTK, which the app needs anyway. No library, no session bus or no Secret Service means unavailable.</summary>
internal sealed class LibsecretKeyring(Func<bool>? hasSessionBus = null) : IKeyring
{
    private const string Secret = "libsecret-1.so.0";
    private const string GLib = "libglib-2.0.so.0";
    private const string SchemaName = "org.readercompanion.DriveLink";
    private const string Attribute = "store";

    // SecretSchema laid out by hand (the constructor is varargs): name, flags, 32 attribute slots {name, type},
    // reserved fields; 592 bytes on 64-bit. Built once and kept for the process.
    private static readonly Lazy<nint> Schema = new(CreateSchema);

    private readonly Func<bool> _hasSessionBus = hasSessionBus ?? HasSessionBus;

    public bool TryLoad(string key, out string? secret)
    {
        string? found = null;
        var available = Call(key, attributes =>
        {
            var value = secret_password_lookupv_sync(Schema.Value, attributes, 0, out var error);
            if (error != 0)
            {
                g_error_free(error);
                return false;
            }

            if (value != 0)
            {
                found = Marshal.PtrToStringUTF8(value);
                secret_password_free(value);
            }

            return true;
        });
        secret = found;
        return available;
    }

    public bool TrySave(string key, string label, string secret) => Call(key, attributes =>
    {
        var stored = secret_password_storev_sync(Schema.Value, attributes, 0, label, secret, 0, out var error);
        if (error != 0)
        {
            g_error_free(error);
            return false;
        }

        return stored != 0;
    });

    public void Delete(string key) => Call(key, attributes =>
    {
        _ = secret_password_clearv_sync(Schema.Value, attributes, 0, out var error);
        if (error != 0)
        {
            g_error_free(error);
        }

        return true;
    });

    /// <summary>Runs <paramref name="work"/> with the attribute table for <paramref name="key"/>; false when the
    /// keyring cannot be reached at all.</summary>
    private bool Call(string key, Func<nint, bool> work)
    {
        if (!OperatingSystem.IsLinux() || IntPtr.Size != 8 || !_hasSessionBus())
        {
            return false;
        }

        try
        {
            var glib = NativeLibrary.Load(GLib);
            var table = g_hash_table_new(NativeLibrary.GetExport(glib, "g_str_hash"), NativeLibrary.GetExport(glib, "g_str_equal"));
            var name = Marshal.StringToCoTaskMemUTF8(Attribute);
            var value = Marshal.StringToCoTaskMemUTF8(key);
            try
            {
                _ = g_hash_table_insert(table, name, value);
                return work(table);
            }
            finally
            {
                g_hash_table_unref(table);
                Marshal.FreeCoTaskMem(name);
                Marshal.FreeCoTaskMem(value);
            }
        }
        catch (Exception e) when (e is DllNotFoundException or EntryPointNotFoundException)
        {
            return false;
        }
    }

    // Without a session bus, libsecret would try to autolaunch one (or wait): treat it as no keyring.
    private static bool HasSessionBus() =>
        !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("DBUS_SESSION_BUS_ADDRESS"))
        || (Environment.GetEnvironmentVariable("XDG_RUNTIME_DIR") is { Length: > 0 } runtime
            && Path.Exists(Path.Combine(runtime, "bus")));

    private static nint CreateSchema()
    {
        const int size = 592;
        var schema = Marshal.AllocHGlobal(size);
        Marshal.Copy(new byte[size], 0, schema, size);
        Marshal.WriteIntPtr(schema, 0, Marshal.StringToCoTaskMemUTF8(SchemaName)); // name; flags (8) = NONE
        Marshal.WriteIntPtr(schema, 16, Marshal.StringToCoTaskMemUTF8(Attribute)); // attributes[0].name; type = STRING
        return schema;
    }

    [DllImport(Secret)]
    private static extern int secret_password_storev_sync(nint schema, nint attributes, nint collection,
        [MarshalAs(UnmanagedType.LPUTF8Str)] string label, [MarshalAs(UnmanagedType.LPUTF8Str)] string password,
        nint cancellable, out nint error);

    [DllImport(Secret)]
    private static extern nint secret_password_lookupv_sync(nint schema, nint attributes, nint cancellable, out nint error);

    [DllImport(Secret)]
    private static extern int secret_password_clearv_sync(nint schema, nint attributes, nint cancellable, out nint error);

    [DllImport(Secret)]
    private static extern void secret_password_free(nint password);

    [DllImport(GLib)]
    private static extern nint g_hash_table_new(nint hash, nint equal);

    [DllImport(GLib)]
    private static extern int g_hash_table_insert(nint table, nint key, nint value);

    [DllImport(GLib)]
    private static extern void g_hash_table_unref(nint table);

    [DllImport(GLib)]
    private static extern void g_error_free(nint error);
}
