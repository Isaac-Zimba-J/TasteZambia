using TasteZambia.API.Media;

namespace TasteZambia.API.Tests.Media;

public class FileSystemMediaStoreTests : IDisposable
{
    private readonly string _root = Path.Combine(Path.GetTempPath(), $"tz-media-{Guid.NewGuid():N}");

    private FileSystemMediaStore Sut() => new(_root);

    public void Dispose()
    {
        if (Directory.Exists(_root)) Directory.Delete(_root, recursive: true);
    }

    [Fact]
    public async Task Save_ThenOpen_ReturnsTheSameBytes()
    {
        var store = Sut();
        var id = Guid.NewGuid();
        var bytes = new byte[] { 1, 2, 3, 4, 5 };

        var stored = await store.SaveAsync(id, "image/jpeg", new MemoryStream(bytes), default);

        Assert.Equal(id, stored.Id);
        Assert.Equal(5, stored.Length);

        await using var read = await store.OpenAsync(id, "image/jpeg", default);
        Assert.NotNull(read);
        using var buffer = new MemoryStream();
        await read!.CopyToAsync(buffer);
        Assert.Equal(bytes, buffer.ToArray());
    }

    [Fact]
    public async Task Open_AMissingBlob_IsNullNotAnException()
    {
        Assert.Null(await Sut().OpenAsync(Guid.NewGuid(), "image/jpeg", default));
    }

    [Fact]
    public async Task Open_WhenTheBlobWasNeverWritten_IsNullEvenWithNoDirectory()
    {
        // Race-condition coverage: DirectoryNotFoundException from a non-existent fan-out path
        // must return null, not throw. This simulates a concurrent delete that removes the
        // directory structure between path construction and file open attempt.
        Assert.Null(await Sut().OpenAsync(Guid.NewGuid(), "image/jpeg", default));
    }

    [Fact]
    public async Task Delete_RemovesIt_AndIsSafeToRepeat()
    {
        var store = Sut();
        var id = Guid.NewGuid();
        await store.SaveAsync(id, "audio/mp4", new MemoryStream([9, 9]), default);

        await store.DeleteAsync(id, "audio/mp4", default);
        await store.DeleteAsync(id, "audio/mp4", default);   // no throw the second time

        Assert.Null(await store.OpenAsync(id, "audio/mp4", default));
    }

    [Fact]
    public async Task AFailedWrite_LeavesNoPartialBlobBehind()
    {
        var store = Sut();
        var id = Guid.NewGuid();

        await Assert.ThrowsAsync<IOException>(() => store.SaveAsync(id, "image/png", new FailingStream(), default));

        // The half-written file must not be readable as a real one.
        Assert.Null(await store.OpenAsync(id, "image/png", default));
        Assert.Empty(Directory.GetFiles(_root, "*", SearchOption.AllDirectories));
    }

    [Fact]
    public void ExtensionFor_MapsEveryAllowedType_AndRefusesTheRest()
    {
        Assert.Equal(".jpg", TasteZambia.Shared.Validation.MediaLimits.ExtensionFor("image/jpeg"));
        Assert.Equal(".m4a", TasteZambia.Shared.Validation.MediaLimits.ExtensionFor("audio/mp4"));
        Assert.Null(TasteZambia.Shared.Validation.MediaLimits.ExtensionFor("application/pdf"));
        Assert.Null(TasteZambia.Shared.Validation.MediaLimits.ExtensionFor("image/svg+xml"));   // scriptable
    }

    /// <summary>Throws part-way through, the way a dropped connection does.</summary>
    private sealed class FailingStream : Stream
    {
        private int _reads;
        public override int Read(byte[] buffer, int offset, int count)
            => _reads++ == 0 ? Fill(buffer, count) : throw new IOException("connection lost");

        public override async ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken ct)
            => _reads++ == 0 ? await Task.FromResult(Math.Min(buffer.Length, 128)) : throw new IOException("connection lost");

        private static int Fill(byte[] buffer, int count) => Math.Min(count, 128);
        public override bool CanRead => true;
        public override bool CanSeek => false;
        public override bool CanWrite => false;
        public override long Length => throw new NotSupportedException();
        public override long Position { get => 0; set => throw new NotSupportedException(); }
        public override void Flush() { }
        public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
        public override void SetLength(long value) => throw new NotSupportedException();
        public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();
    }
}
