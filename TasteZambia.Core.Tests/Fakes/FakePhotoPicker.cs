using TasteZambia.Core.Services;

namespace TasteZambia.Core.Tests.Fakes;

/// <summary>Always says the reader backed out. Nothing in the wizard tests exercises capture yet.</summary>
public sealed class FakePhotoPicker : IPhotoPicker
{
    public Task<PickedFile?> CapturePhotoAsync(CancellationToken ct = default) => Task.FromResult<PickedFile?>(null);
    public Task<PickedFile?> PickPhotoAsync(CancellationToken ct = default) => Task.FromResult<PickedFile?>(null);
}
