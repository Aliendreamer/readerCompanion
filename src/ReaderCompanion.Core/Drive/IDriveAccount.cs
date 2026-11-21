namespace ReaderCompanion.Core.Drive;

/// <summary>The linked Google account: at which level and as whom.</summary>
public sealed record DriveLink(DriveAccess Access, string? AccountName, DateTime LinkedUtc);

/// <summary>Signing in did not give the app access; the message says why, for the user.</summary>
public sealed class DriveSignInException(string message) : Exception(message);

/// <summary>Google no longer accepts the stored link (revoked, or expired); Drive is now unlinked.</summary>
public sealed class DriveLinkLostException() : Exception("Google Drive needs linking again.");

/// <summary>The user's Google Drive link. Tokens are the user's own and stay on this machine.</summary>
public interface IDriveAccount
{
    /// <summary>False when this build has no OAuth client, so Drive cannot be linked.</summary>
    bool IsConfigured { get; }

    /// <summary>The current link, or null when Drive is not linked.</summary>
    DriveLink? Link { get; }

    /// <summary>Raised after linking or unlinking, including when Google drops the link.</summary>
    event Action? Changed;

    /// <summary>Signs in through the browser at <paramref name="access"/>. Throws <see cref="DriveSignInException"/>
    /// when no access was given; cancelling stops waiting for the browser.</summary>
    Task<DriveLink> LinkAsync(DriveAccess access, CancellationToken cancellationToken = default);

    /// <summary>Revokes the token at Google when it can, and always forgets it.</summary>
    Task UnlinkAsync(CancellationToken cancellationToken = default);

    /// <summary>A current access token for Drive calls. Throws <see cref="DriveLinkLostException"/> when Google
    /// rejects the link, and <see cref="InvalidOperationException"/> when not linked.</summary>
    Task<string> GetAccessTokenAsync(CancellationToken cancellationToken = default);
}
