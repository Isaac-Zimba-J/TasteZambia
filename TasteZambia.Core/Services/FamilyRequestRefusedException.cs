namespace TasteZambia.Core.Services;

/// <summary>
/// The archive received the request and refused it. Distinct from an
/// <see cref="HttpRequestException"/>, which means nobody answered - the difference matters
/// because one is something the reader can fix here and the other is their signal.
/// </summary>
public sealed class FamilyRequestRefusedException(string message) : Exception(message);
