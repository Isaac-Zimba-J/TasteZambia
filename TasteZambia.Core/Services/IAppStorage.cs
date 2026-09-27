namespace TasteZambia.Core.Services;

/// <summary>
/// A directory this app owns and the OS does not reclaim under storage pressure — unlike
/// <see cref="Path.GetTempPath"/>, which Android is free to wipe at any time. Anything that
/// must survive until a retry succeeds (a queued upload) belongs here, not in temp.
/// </summary>
public interface IAppStorage
{
    string Directory { get; }
}

/// <summary>
/// A throwaway directory, for tests and previews. Lives here rather than in a test project
/// because both test projects need it, the way InMemoryLocalStore is shared.
/// </summary>
public sealed class TemporaryAppStorage : IAppStorage
{
    public string Directory { get; } = System.IO.Directory.CreateTempSubdirectory("tz-media-").FullName;
}
