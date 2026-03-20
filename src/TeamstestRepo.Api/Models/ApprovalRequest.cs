using System.ComponentModel.DataAnnotations;

namespace TeamstestRepo.Api.Models;

/// <summary>
/// Żądanie zatwierdzenia przychodzące z zewnętrznego systemu.
/// </summary>
public sealed class ApprovalRequest
{
    /// <summary>Unikalny identyfikator procesu w zewnętrznym systemie.</summary>
    [Required]
    public required string ProcessId { get; init; }

    /// <summary>
    /// UPN lub Object ID użytkownika z Microsoft Entra ID
    /// (np. jan.kowalski@firma.pl lub GUID).
    /// </summary>
    [Required]
    public required string UserId { get; init; }

    /// <summary>URL, pod który aplikacja wyśle wynik zatwierdzenia.</summary>
    [Required, Url]
    public required string CallbackUrl { get; init; }

    /// <summary>Etykieta pierwszego pola formularza.</summary>
    [Required, MaxLength(100)]
    public required string Field1Label { get; init; }

    /// <summary>Etykieta drugiego pola formularza.</summary>
    [Required, MaxLength(100)]
    public required string Field2Label { get; init; }

    /// <summary>Opcjonalny opis / tytuł procesu wyświetlany na karcie.</summary>
    [MaxLength(200)]
    public string? ProcessTitle { get; init; }
}
