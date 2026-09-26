namespace TasteZambia.Core.Services;

/// <summary>Records the teacher's voice alongside the written recipe. One recording in flight at a time.</summary>
public interface IVoiceRecorder
{
    Task StartAsync(CancellationToken ct = default);

    /// <summary>Null when nothing was recording; otherwise the file, ready for <see cref="IMediaUploader"/>.</summary>
    Task<PickedFile?> StopAsync(CancellationToken ct = default);

    bool IsRecording { get; }

    /// <summary>How long the current recording has run. Zero when idle.</summary>
    TimeSpan Elapsed { get; }

    /// <summary>
    /// How long this device can record before the file outgrows what the archive accepts.
    /// It depends on the encoding the device actually supports, so it is only meaningful
    /// once recording has started - before that it is the best case.
    /// </summary>
    TimeSpan MaxDuration { get; }
}
