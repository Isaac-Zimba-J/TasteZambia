using TasteZambia.Shared.Validation;

namespace TasteZambia.Core.Tests.Services;

/// <summary>
/// The recorder's duration budget is arithmetic against the archive's ceiling, and it is
/// the thing that decides whether a twelve-minute story can be told. Pinning it here means
/// a change to either number is visible rather than discovered by a 413 on a real recording.
/// </summary>
public class VoiceRecorderBudgetTests
{
    /// <summary>Mirrors MauiVoiceRecorder.Budget - a tenth held back for container overhead.</summary>
    private static TimeSpan Budget(int bytesPerSecond)
        => TimeSpan.FromSeconds(MediaLimits.MaxAudioBytes * 0.9 / bytesPerSecond);

    [Fact]
    public void ThePreferredEncoding_HoldsTheDesignsTwelveMinuteStory()
    {
        // 16 kHz, 16-bit, mono - what telephony and speech recognition use.
        var budget = Budget(32_000);

        Assert.True(budget > TimeSpan.FromMinutes(12),
            $"16 kHz mono must hold a twelve-minute recording; the budget is {budget.TotalMinutes:F1} minutes.");
    }

    [Fact]
    public void TheGuaranteedFallback_IsWhyTheBudgetIsShownToTheReader()
    {
        // 44.1 kHz mono is the only rate Android promises on every device, and it does NOT
        // reach twelve minutes. A phone that can only manage this must say so up front
        // rather than fail on upload - which is what RecordingLimitNote is for.
        var budget = Budget(88_200);

        Assert.True(budget < TimeSpan.FromMinutes(12));
        Assert.True(budget > TimeSpan.FromMinutes(6), "even the worst case should be worth starting");
    }

    [Fact]
    public void EveryBudget_LeavesHeadroomUnderTheCeiling()
    {
        foreach (var bytesPerSecond in new[] { 32_000, 44_100, 88_200 })
        {
            var bytes = Budget(bytesPerSecond).TotalSeconds * bytesPerSecond;
            Assert.True(bytes < MediaLimits.MaxAudioBytes,
                $"{bytesPerSecond} B/s would produce {bytes:N0} bytes, over the {MediaLimits.MaxAudioBytes:N0} ceiling.");
        }
    }
}
