namespace TasteZambia.Core.Services;

/// <summary>
/// Says that something the archive holds has changed, so screens showing it are stale.
///
/// Every screen loads once and returns early afterwards, which is right for an archive
/// that rarely changes - but wrong the moment the reader themselves changes it. Submitting
/// a recipe used to leave the Profile showing the old count until the app was closed and
/// opened again. Writes bump <see cref="Version"/>; a screen records the version it loaded
/// at and reloads when it comes back to find a newer one.
/// </summary>
public interface IArchiveSignal
{
    /// <summary>Increments on every write. A screen compares it with what it last loaded at.</summary>
    int Version { get; }

    /// <summary>Called by whatever wrote. Cheap: it does not reload anything itself.</summary>
    void Changed();
}

public sealed class ArchiveSignal : IArchiveSignal
{
    private int _version;

    public int Version => Volatile.Read(ref _version);

    public void Changed() => Interlocked.Increment(ref _version);
}
