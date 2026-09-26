using TasteZambia.Core.Services;

namespace TasteZambia.Mobile.Services;

/// <summary>App-owned storage backed by the platform's private app data directory.</summary>
public sealed class MauiAppStorage : IAppStorage
{
    public string Directory => FileSystem.AppDataDirectory;
}
