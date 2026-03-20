namespace TeamstestRepo.Api.Models;

/// <summary>
/// Dane przesłane przez Bot Framework po wypełnieniu formularza przez pracownika.
/// </summary>
public sealed class AdaptiveCardPayload
{
    public required string ProcessId { get; init; }
    public required string Decision { get; init; }   // "approve" | "reject"
    public required string Field1 { get; init; }
    public required string Field2 { get; init; }
}
