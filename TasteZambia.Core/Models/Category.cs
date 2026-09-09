namespace TasteZambia.Core.Models;

public sealed record Category(string Name, string? ImageAsset)
{
    public bool HasPhoto => !string.IsNullOrEmpty(ImageAsset);
}
