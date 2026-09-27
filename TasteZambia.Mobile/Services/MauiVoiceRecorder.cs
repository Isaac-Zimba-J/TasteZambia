using Plugin.Maui.Audio;
using TasteZambia.Core.Services;
using TasteZambia.Shared.Validation;

namespace TasteZambia.Mobile.Services;

/// <summary>
/// Records a relative's voice. Stock MAUI has no microphone capture of its own - only
/// MediaPicker, for photographs and video - so this uses Plugin.Maui.Audio.
///
/// The encoding matters more than it looks. At the default 44.1 kHz, 16-bit mono, an
/// uncompressed recording runs at about 88 KB/s and outgrows the archive's 40 MB audio
/// ceiling after eight minutes; the design's own example is a twelve-minute story. Android
/// guarantees only 44.1 kHz and mono on every device, so rather than assume a better
/// encoding works, this tries each in turn and keeps whichever the device accepts - then
/// reports how long that encoding can run before the file is too large to send.
/// </summary>
public sealed class MauiVoiceRecorder(IAppStorage storage) : IVoiceRecorder
{
    /// <summary>
    /// Best first. Speech is intelligible well below CD sample rates - 16 kHz mono is what
    /// telephony and speech recognition use - and the last entry is what Android promises
    /// on every device, so the list cannot come up empty.
    /// </summary>
    private static readonly (AudioRecorderOptions Options, int BytesPerSecond, string ContentType, string Extension)[] Preferences =
    [
        (new AudioRecorderOptions { Encoding = Encoding.Wav, SampleRate = 16_000, Channels = ChannelType.Mono, BitDepth = BitDepth.Pcm16bit, ThrowIfNotSupported = true }, 32_000, "audio/wav", ".wav"),
        (new AudioRecorderOptions { Encoding = Encoding.Wav, SampleRate = 22_050, Channels = ChannelType.Mono, BitDepth = BitDepth.Pcm16bit, ThrowIfNotSupported = true }, 44_100, "audio/wav", ".wav"),
        (new AudioRecorderOptions { Encoding = Encoding.Wav, SampleRate = 44_100, Channels = ChannelType.Mono, BitDepth = BitDepth.Pcm16bit, ThrowIfNotSupported = false }, 88_200, "audio/wav", ".wav"),
    ];

    private IAudioRecorder? _recorder;
    private string? _path;
    private string _contentType = "audio/wav";
    private DateTimeOffset _startedAt;

    public bool IsRecording => _recorder?.IsRecording ?? false;

    public TimeSpan Elapsed => IsRecording ? DateTimeOffset.UtcNow - _startedAt : TimeSpan.Zero;

    public TimeSpan MaxDuration { get; private set; } = Budget(Preferences[0].BytesPerSecond);

    public async Task StartAsync(CancellationToken ct = default)
    {
        var status = await Permissions.CheckStatusAsync<Permissions.Microphone>();
        if (status != PermissionStatus.Granted)
            status = await Permissions.RequestAsync<Permissions.Microphone>();
        if (status != PermissionStatus.Granted)
            throw new UnauthorizedAccessException("Microphone permission was not granted.");

        System.IO.Directory.CreateDirectory(storage.Directory);

        foreach (var (options, bytesPerSecond, contentType, extension) in Preferences)
        {
            var path = Path.Combine(storage.Directory, $"tz-recording-{Guid.NewGuid():N}{extension}");
            try
            {
                var recorder = AudioManager.Current.CreateRecorder(options);
                await recorder.StartAsync(path);

                _recorder = recorder;
                _path = path;
                _contentType = contentType;
                _startedAt = DateTimeOffset.UtcNow;
                MaxDuration = Budget(bytesPerSecond);
                return;
            }
            catch (Exception)
            {
                // This device will not record at that quality. Try the next, and leave no
                // empty file behind from the attempt.
                if (File.Exists(path)) File.Delete(path);
            }
        }

        throw new InvalidOperationException("This device would not start a recording at any supported quality.");
    }

    public async Task<PickedFile?> StopAsync(CancellationToken ct = default)
    {
        if (_recorder is null || _path is null) return null;

        if (_recorder.IsRecording) await _recorder.StopAsync();

        var path = _path;
        var contentType = _contentType;
        _path = null;
        _recorder = null;

        if (!File.Exists(path)) return null;

        // The caller deletes this file once the bytes are safely uploaded or queued - the
        // uploader keeps its own copy, so leaving this one behind would be pure litter.
        return new PickedFile(Path.GetFileName(path), contentType, _ => Task.FromResult<Stream>(File.OpenRead(path)))
        {
            LocalPath = path,
        };
    }

    /// <summary>
    /// How long this encoding can run inside the archive's ceiling, with a tenth held back
    /// for the container's own overhead and for stopping a moment late.
    /// </summary>
    private static TimeSpan Budget(int bytesPerSecond)
        => TimeSpan.FromSeconds(MediaLimits.MaxAudioBytes * 0.9 / bytesPerSecond);
}
