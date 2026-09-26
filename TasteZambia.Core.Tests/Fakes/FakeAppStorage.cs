using TasteZambia.Core.Services;

namespace TasteZambia.Core.Tests.Fakes;

/// <summary>A fresh temp folder per instance, standing in for the platform's app-owned directory.</summary>
public sealed class FakeAppStorage : IAppStorage
{
    public string Directory { get; } = System.IO.Directory.CreateTempSubdirectory("tz-media-tests-").FullName;
}
