namespace ReaderCompanion.Data;

/// <summary>The system's store for secrets (on Linux, GNOME Keyring or KWallet). Every call reports whether a
/// keyring could be used at all, so callers can fall back to something else.</summary>
internal interface IKeyring
{
    /// <summary>False when no keyring can be used; otherwise true, with the stored secret or null.</summary>
    bool TryLoad(string key, out string? secret);

    /// <summary>False when no keyring can be used or it refused the secret.</summary>
    bool TrySave(string key, string label, string secret);

    /// <summary>Removes the secret if there is one; does nothing when no keyring can be used.</summary>
    void Delete(string key);
}
