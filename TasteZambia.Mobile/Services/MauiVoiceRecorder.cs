using Plugin.Maui.Audio;
using TasteZambia.Core.Services;

namespace TasteZambia.Mobile.Services;

/// <summary>
/// Records to a Wav file with Plugin.Maui.Audio - stock MAUI has no microphone capture API of
/// its own, only MediaPicker for photos and video. Wav (rather than AAC) is what every target
/// platform here supports without an Android 12+ floor, and "audio/wav" is already one of the
/// archive's accepted types.
/// </summary>
public sealed class MauiVoiceRecorder(IAppStorage storage) : IVoiceRecorder
{
    private readonly IAudioRecorder _recorder = AudioManager.Current.CreateRecorder(new AudioRecorderOptions { Encoding = Encoding.Wav });
    private string? _path;
    private DateTimeOffset _startedAt;

    public bool IsRecording => _recorder.IsRecording;
    public TimeSpan Elapsed => IsRecording ? DateTimeOffset.UtcNow - _startedAt : TimeSpan.Zero;

    public async Task StartAsync(CancellationToken ct = default)
    {
        var status = await Permissions.CheckStatusAsync<Permissions.Microphone>();
        if (status != PermissionStatus.Granted)
            status = await Permissions.RequestAsync<Permissions.Microphone>();
        if (status != PermissionStatus.Granted)
            throw new UnauthorizedAccessException("Microphone permission was not granted.");

        System.IO.Directory.CreateDirectory(storage.Directory);
        _path = Path.Combine(storage.Directory, $"tz-recording-{Guid.NewGuid():N}.wav");
        _startedAt = DateTimeOffset.UtcNow;
        await _recorder.StartAsync(_path);
    }

    public async Task<PickedFile?> StopAsync(CancellationToken ct = default)
    {
        if (!IsRecording || _path is null) return null;

        await _recorder.StopAsync();
        var path = _path;
        _path = null;
        return new PickedFile(Path.GetFileName(path), "audio/wav", _ => Task.FromResult<Stream>(File.OpenRead(path)));
    }
}
