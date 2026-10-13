using System.ComponentModel.DataAnnotations;

namespace UPZMG.Persistence.Models;

/// <summary>
/// Security module.
/// What one user did with one alert of the intelligent layer: saw it, or dismissed it.
/// The alerts themselves are not stored; they are detected from the series on every read.
/// </summary>

public class AlertReceipt
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; } //fk
    [MaxLength(120)]
    public string AlertKey { get; set; } = null!;
    public DateTime? SeenAtUtc { get; set; }
    public DateTime? DismissedAtUtc { get; set; }
}
